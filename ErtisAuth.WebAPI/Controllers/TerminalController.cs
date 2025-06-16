using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Authorized]
[RbacResource("terminal")]
[Route("api/v{v:apiVersion}/terminal")]
public class TerminalController : ControllerBase
{
    #region Methods
    
    [HttpPost]
    [RbacAction(Rbac.CrudActions.Read)]
    public async Task<IActionResult> ExecuteCommand([FromBody] TerminalRequest model, CancellationToken cancellationToken = default)
    {
        var utilizer = this.GetUtilizer();
        if (utilizer.TokenType != SupportedTokenTypes.Bearer)
        {
            return this.Forbid();
        }
        
        if (string.IsNullOrEmpty(model.Command))
        {
            return this.CommandRequired();
        }
        
        var parts = model.Command.Split(' ');
        if (parts.Length > 0)
        {
            var command = parts.First();
            var arguments = string.Join(" ", parts.Skip(1));
            
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                
                var process = new Process
                {
                    StartInfo = processStartInfo
                };
                
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync(cancellationToken: cancellationToken);
                await process.WaitForExitAsync(cancellationToken: cancellationToken);
                
                return this.Content(output, "text/plain");
            }
            catch (Win32Exception ex)
            {
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (ex.Message.Contains("No such file or directory"))
                {
                    return this.Content($"Command not found: {command}", "text/plain");
                }
                
                return this.Content(ex.Message, "text/plain");
            }
            catch (Exception ex)
            {
                return this.Content(ex.Message, "text/plain");
            }
        }
        else
        {
            return this.CommandRequired();
        }
    }
    
    #endregion
    
    #region Helper Classes
    
    public class TerminalRequest
    {
        #region Properties
        
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string Command { get; set; }
        
        #endregion
    }
    
    #endregion
}