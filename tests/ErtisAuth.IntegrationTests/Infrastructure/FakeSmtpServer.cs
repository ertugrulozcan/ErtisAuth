using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MimeKit;

// ReSharper disable MemberCanBePrivate.Global
namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// A minimal SMTP server on a random loopback port that accepts every mail (and any credentials) and keeps it,
/// so that tests can read what ErtisAuth sent through its SmtpServer mail provider (MailKit).
/// No STARTTLS is offered, so the client stays on the plain connection.
/// </summary>
public sealed class FakeSmtpServer : IAsyncDisposable
{
	#region Fields
	
	private readonly TcpListener _listener;
	
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	
	private readonly ConcurrentQueue<MimeMessage> _messages = new();
	
	private readonly Task _acceptLoop;
	
	#endregion
	
	#region Properties
	
	public string Host => "127.0.0.1";
	
	public int Port { get; }
	
	public IReadOnlyCollection<MimeMessage> Messages => this._messages.ToArray();
	
	#endregion
	
	#region Constructors
	
	public FakeSmtpServer()
	{
		this._listener = new TcpListener(IPAddress.Loopback, 0);
		this._listener.Start();
		this.Port = ((IPEndPoint) this._listener.LocalEndpoint).Port;
		this._acceptLoop = this.AcceptLoopAsync(this._cancellationTokenSource.Token);
	}
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Waits for a mail to the given address (mails are sent in the background by ErtisAuth).
	/// </summary>
	public async Task<MimeMessage> WaitForMessageAsync(string recipient, Func<MimeMessage, bool>? predicate = null, TimeSpan? timeout = null)
	{
		var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
		while (DateTime.UtcNow < deadline)
		{
			var message = this.MessagesTo(recipient).FirstOrDefault(x => predicate == null || predicate(x));
			if (message != null)
			{
				return message;
			}
			
			await Task.Delay(50, TestContext.Current.CancellationToken);
		}
		
		throw new TimeoutException($"No mail to {recipient} within the timeout. Received: {string.Join(", ", this.Messages.Select(x => $"{x.To} '{x.Subject}'"))}");
	}
	
	public IEnumerable<MimeMessage> MessagesTo(string recipient)
	{
		return this.Messages.Where(x => x.To.Mailboxes.Any(y => string.Equals(y.Address, recipient, StringComparison.OrdinalIgnoreCase)));
	}
	
	private async Task AcceptLoopAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			TcpClient client;
			try
			{
				client = await this._listener.AcceptTcpClientAsync(cancellationToken);
			}
			catch (Exception) when (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			
			_ = this.HandleClientAsync(client, cancellationToken);
		}
	}
	
	private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
	{
		using (client)
		{
			try
			{
				var stream = client.GetStream();
				using var reader = new StreamReader(stream, Encoding.UTF8);
				await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
				writer.NewLine = "\r\n";
				writer.AutoFlush = true;
				
				await writer.WriteLineAsync("220 localhost Fake ESMTP");
				while (await reader.ReadLineAsync(cancellationToken) is { } line)
				{
					var command = line.Split(' ', 2)[0].ToUpperInvariant();
					switch (command)
					{
						case "EHLO":
						{
							await writer.WriteLineAsync("250-localhost");
							await writer.WriteLineAsync("250 AUTH PLAIN");
							break;
						}
						case "AUTH":
						{
							// "AUTH PLAIN <initial response>" or "AUTH PLAIN" followed by the response
							if (line.Split(' ').Length < 3)
							{
								await writer.WriteLineAsync("334 ");
								await reader.ReadLineAsync(cancellationToken);
							}
							
							await writer.WriteLineAsync("235 2.7.0 Authentication successful");
							break;
						}
						case "DATA":
						{
							await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
							var data = new StringBuilder();
							while (await reader.ReadLineAsync(cancellationToken) is { } dataLine && dataLine != ".")
							{
								// Dot-stuffing
								data.Append(dataLine.StartsWith("..") ? dataLine[1..] : dataLine).Append("\r\n");
							}
							
							using var messageStream = new MemoryStream(Encoding.UTF8.GetBytes(data.ToString()));
							this._messages.Enqueue(await MimeMessage.LoadAsync(messageStream, cancellationToken));
							await writer.WriteLineAsync("250 2.0.0 Ok: queued");
							break;
						}
						case "QUIT":
						{
							await writer.WriteLineAsync("221 2.0.0 Bye");
							return;
						}
						default:
						{
							// HELO, MAIL FROM, RCPT TO, RSET, NOOP
							await writer.WriteLineAsync("250 2.0.0 Ok");
							break;
						}
					}
				}
			}
			catch (Exception) when (cancellationToken.IsCancellationRequested)
			{
				// Disposed while a client was connected
			}
			catch (IOException)
			{
				// The client closed the connection
			}
		}
	}
	
	public async ValueTask DisposeAsync()
	{
		await this._cancellationTokenSource.CancelAsync();
		this._listener.Stop();
		await this._acceptLoop;
		this._cancellationTokenSource.Dispose();
	}
	
	#endregion
}