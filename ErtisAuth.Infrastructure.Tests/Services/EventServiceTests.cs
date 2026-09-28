using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Events are stored and then published to the subscribers (webhooks, mail hooks) synchronously.
/// </summary>
public class EventServiceTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
	
	private readonly List<ErtisAuthEvent> _events;
	
	#endregion
	
	#region Constructors
	
	public EventServiceTests()
	{
		this._events = InMemoryRepository.Setup(this._repository, x => x.Id ??= ObjectId.GenerateNewId().ToString());
		
		var membership = TestServiceFactory.CreateMembership();
		membership.Id = MembershipId;
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(membership);
	}
	
	#endregion
	
	#region Helpers
	
	private EventService CreateService()
	{
		return new EventService(this._membershipService, this._repository, NullLogger<EventService>.Instance);
	}
	
	private static Utilizer CreateUtilizer(string membershipId = MembershipId)
	{
		return new Utilizer { Id = UserId, Username = "john.doe", Type = Utilizer.UtilizerType.User, MembershipId = membershipId };
	}
	
	#endregion
	
	#region Fire
	
	[Fact]
	public async Task FireEventAsync_StoresTheEventAndPublishesIt()
	{
		var service = this.CreateService();
		ErtisAuthEvent? published = null;
		service.OnEventFired += (_, x) => published = x;
		
		var fired = await service.FireEventAsync(ErtisAuthEventType.UserCreated, CreateUtilizer(), MembershipId, new { username = "john.doe" }, new { username = "old" }, TestContext.Current.CancellationToken);
		
		var stored = Assert.Single(this._events);
		Assert.Same(stored, fired);
		Assert.Same(stored, published);
		Assert.Equal(ErtisAuthEventType.UserCreated, stored.EventType);
		Assert.Equal(UserId, stored.UtilizerId);
		Assert.Equal(MembershipId, stored.MembershipId);
		Assert.Equal("john.doe", stored.BsonDocument?["username"].AsString);
		Assert.Equal("old", stored.BsonPrior?["username"].AsString);
		Assert.True(DateTime.UtcNow - stored.EventTime < TimeSpan.FromMinutes(1));
	}
	
	[Fact]
	public async Task FireEventAsync_WithoutMembershipId_UsesTheUtilizersMembership()
	{
		await this.CreateService().FireEventAsync(ErtisAuthEventType.UserCreated, CreateUtilizer(), null, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal(MembershipId, Assert.Single(this._events).MembershipId);
	}
	
	[Fact]
	public async Task FireEventAsync_WithUtilizerId_StoresTheEvent()
	{
		await this.CreateService().FireEventAsync(ErtisAuthEventType.UserPasswordChanged, UserId, MembershipId, cancellationToken: TestContext.Current.CancellationToken);
		
		var stored = Assert.Single(this._events);
		Assert.Equal(UserId, stored.UtilizerId);
		Assert.Equal(ErtisAuthEventType.UserPasswordChanged, stored.EventType);
	}
	
	[Fact]
	public async Task FireEventAsync_WhenASubscriberFails_StillSucceedsAndNotifiesTheOthers()
	{
		var service = this.CreateService();
		var notified = new List<string>();
		service.OnEventFired += (_, _) => notified.Add("first");
		service.OnEventFired += (_, _) => throw new InvalidOperationException("subscriber failure");
		service.OnEventFired += (_, _) => notified.Add("third");
		
		await service.FireEventAsync(ErtisAuthEventType.UserCreated, CreateUtilizer(), MembershipId, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Single(this._events);
		Assert.Equal(["first", "third"], notified);
	}
	
	#endregion
	
	#region Read
	
	[Fact]
	public async Task GetAsync_ReturnsOnlyEventsOfTheMembership()
	{
		var service = this.CreateService();
		var own = await service.FireEventAsync(ErtisAuthEventType.UserCreated, CreateUtilizer(), MembershipId, cancellationToken: TestContext.Current.CancellationToken);
		var other = await service.FireEventAsync(ErtisAuthEventType.UserCreated, CreateUtilizer("5f8a1b2c3d4e5f6a7b8c9dff"), "5f8a1b2c3d4e5f6a7b8c9dff", cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.NotNull(await service.GetAsync(MembershipId, own.Id, TestContext.Current.CancellationToken));
		Assert.Null(await service.GetAsync(MembershipId, other.Id, TestContext.Current.CancellationToken));
		
		var events = await service.GetAsync(MembershipId, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(own.Id, Assert.Single(events.Items).Id);
	}
	
	[Fact]
	public async Task GetAsync_WithUnknownMembership_ThrowsMembershipNotFound()
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateService().GetAsync("5f8a1b2c3d4e5f6a7b8c9dff", "5f8a1b2c3d4e5f6a7b8c9d10", TestContext.Current.CancellationToken));
		
		Assert.Equal("MembershipNotFound", exception.ErrorCode);
	}
	
	#endregion
}