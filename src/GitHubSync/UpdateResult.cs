public record UpdateResult(
    string Url,
    string CommitSha,
    string? BranchName,
    int? PullRequestId
);