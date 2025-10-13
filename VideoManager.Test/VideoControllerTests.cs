// =====================================
// Unit Tests — VideoControllerTests.cs
// =====================================
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VideoManager.Api.Application.Interfaces;
using VideoManager.Api.Controllers;
using VideoManager.Api.Domain.Common;
using VideoManager.Api.Domain.Models;
using VideoManager.Api.DTOs;
using Xunit;


namespace VideoManager.Test;


public class VideoControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOk_WithServiceResult()
    {
        var svc = new Mock<IVideoService>();
        svc.Setup(static s => s.GetAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new PagedResult<Video>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        });


        var sut = new VideoController(svc.Object);
        var res = await sut.GetAll(null, null, null, 1, 10, default);


        res.Should().BeOfType<OkObjectResult>()
        .Which.Value.Should().BeAssignableTo<PagedResult<Video>>();
    }


    [Fact]
    public async Task GetById_ReturnsNotFound_WhenNull()
    {
        var svc = new Mock<IVideoService>();
        svc.Setup(s => s.GetByIdAsync(42, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Video?)null);


        var sut = new VideoController(svc.Object);
        var res = await sut.GetById(42, default);
        res.Should().BeOfType<NotFoundResult>();
    }


    [Fact]
    public async Task Create_ReturnsCreatedAt_WhenValid()
    {
        var svc = new Mock<IVideoService>();
        var created = new Video { Id = 7, Title = "t" };
        svc.Setup(s => s.CreateAsync(It.IsAny<CreateVideoRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(created);
        var sut = new VideoController(svc.Object);


        var res = await sut.Create(new CreateVideoRequest { Title = "t" }, default);
        res.Should().BeOfType<CreatedAtActionResult>()
        .Which.RouteValues!["id"].Should().Be(7);
    }


    [Fact]
    public void UpdateReturnsOkWithUpdated()
    {
        var svc = new Mock<IVideoService>();
        var updated = new Video { Id = 5, Title = "updated" };
        svc.Setup(s => s.UpdateAsync(5, It.IsAny<UpdateVideoRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(updated);
    }
}
