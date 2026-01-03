using System.Security.Claims;
using BrickDex.Core.Contracts;
using BrickDex.Web.Contracts;

namespace BrickDex.Web.Endpoints;

public static class UserSetEndpoints {
    public static WebApplication MapUserSetEndpoints(this WebApplication app) {
        var group = app.MapGroup("/api/usersets").RequireAuthorization();

        group.MapPost("/{id:guid}/status", UpdateStatusAsync);
        group.MapPost("/collection", AddToCollectionAsync);
        group.MapPost("/wishlist", AddToWishlistAsync);

        return app;
    }

    private static async Task<IResult> UpdateStatusAsync(
        Guid id,
        UpdateStatusRequest request,
        IUserSetService userSetService,
        IUserService userService,
        ClaimsPrincipal user) {
        var currentUser = await userService.GetCurrentUserAsync(user);
        if(currentUser == null) {
            return Results.Unauthorized();
        }

        var userSet = await userSetService.GetUserSetAsync(currentUser.Id, id);
        if(userSet == null) {
            return Results.NotFound();
        }

        userSet.Status = request.Status;
        await userSetService.UpdateUserSetAsync(userSet);

        return Results.Ok(new { status = userSet.Status.ToString() });
    }

    private static async Task<IResult> AddToCollectionAsync(
        AddSetRequest request,
        IUserSetService userSetService,
        IUserService userService,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken cancellationToken) {
        var currentUser = await userService.GetCurrentUserAsync(user, cancellationToken);
        if(currentUser == null) {
            return Results.Unauthorized();
        }

        try {
            var userSet = await userSetService.AddToUserCollectionAsync(
                currentUser.Id, request.SetNumber, isWishlist: false, cancellationToken);
            return Results.Ok(new AddSetResponse(userSet.Id, userSet.SetNumber, userSet.Set.Name));
        } catch(InvalidOperationException ex) {
            logger.LogWarning(ex, "Failed to add set {SetNumber} to collection", request.SetNumber);
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> AddToWishlistAsync(
        AddSetRequest request,
        IUserSetService userSetService,
        IUserService userService,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken cancellationToken) {
        var currentUser = await userService.GetCurrentUserAsync(user, cancellationToken);
        if(currentUser == null) {
            return Results.Unauthorized();
        }

        try {
            var userSet = await userSetService.AddToUserCollectionAsync(
                currentUser.Id, request.SetNumber, isWishlist: true, cancellationToken);
            return Results.Ok(new AddSetResponse(userSet.Id, userSet.SetNumber, userSet.Set.Name));
        } catch(InvalidOperationException ex) {
            logger.LogWarning(ex, "Failed to add set {SetNumber} to wishlist", request.SetNumber);
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
