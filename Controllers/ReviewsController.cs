using ListamCompetitor.Api.Dtos;
using ListamCompetitor.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ListamCompetitor.Api.Auth;

namespace ListamCompetitor.Api.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewsService _service;

    public ReviewsController(IReviewsService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();


        var dto = await _service.CreateAsync(userId, request, ct);
        return Ok(dto);
    }

    [AllowAnonymous]
    [HttpGet("user/{userId:guid}/rating")]
    public async Task<ActionResult<UserRatingDto>> GetRating(Guid userId, CancellationToken ct)
    {
        return Ok(await _service.GetUserRatingAsync(userId, ct));
    }

    [AllowAnonymous]
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<ReviewDto>>> GetUserReviews(Guid userId, CancellationToken ct)
    {
        return Ok(await _service.GetUserReviewsAsync(userId, ct));
    }
}
