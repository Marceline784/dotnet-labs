using AutoMapper;
using Feedback.Application.Feedbacks.Commands;
using Feedback.Application.Feedbacks.Commands.AddComment;
using Feedback.Application.Feedbacks.Commands.CreateFeedback;
using Feedback.Application.Feedbacks.Commands.UpdateFeedback;
using Feedback.WebApi.Contracts;
using Feedback.WebApi.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Feedback.Tests;

/// <summary>Конфігурація AutoMapper перевіряється в тесті (AssertConfigurationIsValid), а не під час першого запиту.</summary>
public class WebApiMappingTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<FeedbackProfile>());
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public void Mapping_configuration_is_valid()
    {
        CreateMapper().ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void Create_request_maps_to_command()
    {
        var request = new CreateFeedbackRequest
        {
            OutfitId = 1, AuthorUserId = 2, AuthorName = "Anna", Feeling = "Cold", Rating = 4, Text = "Cool",
            Items = new List<ItemSnapshotInput> { new(3, "Sweater", "Top") },
            TemperatureNote = new TemperatureNoteInput(-2, 3),
            Photos = new List<string> { "a.jpg" }
        };

        var command = CreateMapper().Map<CreateFeedbackCommand>(request);

        Assert.Equal(1, command.OutfitId);
        Assert.Equal("Anna", command.AuthorName);
        Assert.Equal(4, command.Rating);
        Assert.Single(command.Items!);
        Assert.Equal(-2, command.TemperatureNote!.Felt);
        Assert.Equal("a.jpg", command.Photos!.Single());
    }

    [Fact]
    public void Update_request_takes_id_and_version_from_context()
    {
        var command = CreateMapper().Map<UpdateFeedbackCommand>(
            new UpdateFeedbackRequest { Feeling = "Hot", Rating = 4 },
            o =>
            {
                o.Items[FeedbackProfile.IdKey] = "66f0c2a1b2c3d4e5f6000001";
                o.Items[FeedbackProfile.VersionKey] = 3;
            });

        Assert.Equal("66f0c2a1b2c3d4e5f6000001", command.Id);
        Assert.Equal(3, command.ExpectedVersion);
        Assert.Equal("Hot", command.Feeling);
    }

    [Fact]
    public void Missing_version_means_any_version()
    {
        var command = CreateMapper().Map<AddCommentCommand>(
            new AddCommentRequest { AuthorName = "Max", Text = "Agree" },
            o => o.Items[FeedbackProfile.IdKey] = "66f0c2a1b2c3d4e5f6000001");

        Assert.Null(command.ExpectedVersion);
        Assert.Equal("Agree", command.Text);
    }
}
