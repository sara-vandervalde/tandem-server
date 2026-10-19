using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tandem.API.Extensions;
using Tandem.Service.Authorization;

namespace Tandem.API.Filters;

// Custom action filter: runs before the action and lets it run only for members of the group in the route
// (or the Admin, who may view every group). Non-members get 403, a missing group 404 - the action never starts.
// Usage: [RequireGroupMember] on an action whose route has {groupId}
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireGroupMemberAttribute : TypeFilterAttribute
{
    public RequireGroupMemberAttribute(string routeKey = "groupId") : base(typeof(RequireGroupMemberFilter))
    {
        Arguments = [routeKey];
    }

    // TypeFilterAttribute creates this through DI, so it can receive GroupAccessService in its constructor
    private class RequireGroupMemberFilter : IAsyncActionFilter
    {
        private readonly GroupAccessService _groupAccess;
        private readonly string _routeKey;

        public RequireGroupMemberFilter(GroupAccessService groupAccess, string routeKey)
        {
            _groupAccess = groupAccess;
            _routeKey = routeKey;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!int.TryParse(context.RouteData.Values[_routeKey]?.ToString(), out var groupId))
            {
                context.Result = new BadRequestResult();
                return;
            }

            var user = context.HttpContext.User;
            var access = await _groupAccess.EnsureMemberOrAdminAsync(
                groupId, user.GetUserId(), user.IsInRole("Admin"), context.HttpContext.RequestAborted);

            if (!access.IsSuccess)
            {
                // Setting a Result stops the pipeline: the action does not run
                context.Result = ((ControllerBase)context.Controller).ToFailureResult(access);
                return;
            }

            await next();
        }
    }
}
