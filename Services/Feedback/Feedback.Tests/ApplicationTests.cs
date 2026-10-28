
using Feedback.Application.Common.Behaviors;
using Feedback.Application.Common.Validation;
using Feedback.Application.Feedbacks.Commands;
using Feedback.Application.Feedbacks.Commands.AddComment;
using Feedback.Application.Feedbacks.Commands.CreateFeedback;
using Feedback.Application.Feedbacks.Commands.DeleteFeedback;
using Feedback.Application.Feedbacks.Commands.UpdateFeedback;
using Feedback.Application.Feedbacks.Queries.GetFeedbackById;
using Feedback.Domain.Entities;
using Feedback.Domain.Enums;
using Feedback.Domain.Exceptions;
using Feedback.Domain.Interfaces;
using Feedback.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Xunit;

namespace Feedback.Tests;

/// <summary>Тести шару Application без MongoDB: репозиторій замінено простим двійником (fake).</summary>
public class ApplicationTests
{
    private sealed class FakeFeedbackRepository : IFeedbackRepository
    {
        public List<OutfitFeedback> Store { get; } = new();
        public bool ExistsResult { get; set; }

        public Task<OutfitFeedback?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(f => f.Id == id));

        public Task<bool> ExistsAsync(long outfitId, long authorUserId, CancellationToken ct = default)
            => Task.FromResult(ExistsResult || Store.Any(f => f.OutfitId == outfitId && f.Author.UserId == authorUserId));

        public Task AddAsync(OutfitFeedback feedback, CancellationToken ct = default)
        {
            Store.Add(feedback);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OutfitFeedback feedback, CancellationToken ct = default)
        {
            feedback.BumpVersion();
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default)
            => Task.FromResult(Store.RemoveAll(f => f.Id == id) > 0);
    }

    private static CreateFeedbackCommand ValidCommand(int? rating = 5)
        => new(OutfitId: 1, AuthorUserId: 1, AuthorName: "Anna", Feeling: "Comfortable", Rating: rating, Text: "Great");

    private static ValidationBehavior<CreateFeedbackCommand, string> Pipeline(FakeFeedbackRepository repo)
        => new(new IValidator<CreateFeedbackCommand>[]
        {
            new CreateFeedbackCommandValidator(repo)
        });

    // ---------- ValidationBehavior: некоректні дані відхиляються ДО обробника ----------

    [Fact]
    public async Task Rating_ten_is_rejected_by_pipeline_and_handler_never_runs()
    {
        var repo = new FakeFeedbackRepository();
        var handlerRan = false;

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            Pipeline(repo).Handle(
                ValidCommand(rating: 10),
                _ =>
                {
                    handlerRan = true;
                    return Task.FromResult("id");
                },
                CancellationToken.None));

        Assert.False(handlerRan);
        Assert.Contains(ex.Errors, e => e.PropertyName == "Rating");
    }

    [Fact]
    public async Task All_errors_are_collected_together()
    {
        var command = new CreateFeedbackCommand(
            OutfitId: 0,
            AuthorUserId: 0,
            AuthorName: "",
            Feeling: "Freezing",
            Rating: 9);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            Pipeline(new FakeFeedbackRepository()).Handle(
                command,
                _ => Task.FromResult("id"),
                CancellationToken.None));

        Assert.True(ex.Errors.Count() >= 4);
    }

    [Fact]
    public async Task Valid_command_reaches_handler_and_is_stored()
    {
        var repo = new FakeFeedbackRepository();
        var handler = new CreateFeedbackCommandHandler(
            repo,
            NullLogger<CreateFeedbackCommandHandler>.Instance);

        var command = ValidCommand();

        var id = await Pipeline(repo).Handle(
            command,
            ct => handler.Handle(command, ct),
            CancellationToken.None);

        Assert.True(ObjectId.TryParse(id, out _));
        Assert.Single(repo.Store);
        Assert.Equal(Feeling.Comfortable, repo.Store[0].Feeling);
    }

    [Fact]
    public async Task Duplicate_feedback_is_rejected_by_async_validator()
    {
        var repo = new FakeFeedbackRepository { ExistsResult = true };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            Pipeline(repo).Handle(
                ValidCommand(),
                _ => Task.FromResult("id"),
                CancellationToken.None));

        Assert.Contains(ex.Errors, e => e.ErrorMessage.Contains("already left feedback"));
    }

    [Fact]
    public void Invalid_object_id_is_rejected_by_query_validator()
    {
        var validator = new GetFeedbackByIdQueryValidator();

        Assert.False(validator.Validate(
            new GetFeedbackByIdQuery("not-an-object-id")).IsValid);

        Assert.True(validator.Validate(
            new GetFeedbackByIdQuery(ObjectId.GenerateNewId().ToString())).IsValid);
    }

    [Fact]
    public void Delete_validator_checks_object_id()
    {
        Assert.False(new DeleteFeedbackCommandValidator().Validate(
            new DeleteFeedbackCommand("123")).IsValid);
    }

    [Fact]
    public void Comment_command_requires_text()
    {
        var validator = new AddCommentCommandValidator();

        Assert.False(validator.Validate(
            new AddCommentCommand(
                ObjectId.GenerateNewId().ToString(),
                "Max",
                "")).IsValid);
    }

    // ---------- Обробники команд ----------

    private static OutfitFeedback Stored(FakeFeedbackRepository repo)
    {
        var feedback = new OutfitFeedback(
            1,
            new Author(1, "Anna"),
            Feeling.Cold,
            3,
            "Cool");

        repo.Store.Add(feedback);
        return feedback;
    }

    [Fact]
    public async Task Update_with_outdated_version_is_a_conflict()
    {
        var repo = new FakeFeedbackRepository();
        var feedback = Stored(repo);

        var handler = new UpdateFeedbackCommandHandler(
            repo,
            NullLogger<UpdateFeedbackCommandHandler>.Instance);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new UpdateFeedbackCommand(
                feedback.Id.ToString(),
                "Hot",
                4,
                ExpectedVersion: 7),
            CancellationToken.None));
    }

    [Fact]
    public async Task Update_with_matching_version_returns_new_version()
    {
        var repo = new FakeFeedbackRepository();
        var feedback = Stored(repo);

        var handler = new UpdateFeedbackCommandHandler(
            repo,
            NullLogger<UpdateFeedbackCommandHandler>.Instance);

        var version = await handler.Handle(
            new UpdateFeedbackCommand(
                feedback.Id.ToString(),
                "Hot",
                4,
                "Warm",
                ExpectedVersion: 0),
            CancellationToken.None);

        Assert.Equal(1, version);
        Assert.Equal(Feeling.Hot, feedback.Feeling);
    }

    [Fact]
    public async Task Update_of_unknown_document_is_not_found()
    {
        var handler = new UpdateFeedbackCommandHandler(
            new FakeFeedbackRepository(),
            NullLogger<UpdateFeedbackCommandHandler>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new UpdateFeedbackCommand(
                ObjectId.GenerateNewId().ToString(),
                "Hot"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Delete_of_unknown_document_is_not_found()
    {
        var handler = new DeleteFeedbackCommandHandler(
            new FakeFeedbackRepository(),
            NullLogger<DeleteFeedbackCommandHandler>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new DeleteFeedbackCommand(
                ObjectId.GenerateNewId().ToString()),
            CancellationToken.None));
    }

    [Fact]
    public async Task Sixth_comment_is_rejected_by_the_domain()
    {
        var repo = new FakeFeedbackRepository();
        var feedback = Stored(repo);

        var handler = new AddCommentCommandHandler(
            repo,
            NullLogger<AddCommentCommandHandler>.Instance);

        for (var i = 0; i < OutfitFeedback.MaxComments; i++)
        {
            await handler.Handle(
                new AddCommentCommand(
                    feedback.Id.ToString(),
                    "Max",
                    $"Comment {i}"),
                CancellationToken.None);
        }

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new AddCommentCommand(
                feedback.Id.ToString(),
                "Max",
                "One too many"),
            CancellationToken.None));
    }

    [Fact]
    public void Feeling_parser_rejects_numbers_and_unknown_names()
    {
        Assert.True(FeelingParser.IsValid("cold"));
        Assert.False(FeelingParser.IsValid("1"));
        Assert.False(FeelingParser.IsValid("Freezing"));
    }
}
