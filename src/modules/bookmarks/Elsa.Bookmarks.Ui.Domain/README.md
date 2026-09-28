# Elsa.Bookmarks.Ui.Domain

The shape of the bookmark UI, without Elsa.

This package holds the models (`BookmarkUiView`, `BookmarkUiContext`, `BookmarkResumeSchema` and what
belongs with them), the contracts (`IBookmarkUiProvider`, `IBookmarkUiMapper`, `IBookmarkResumeWriter`)
and two helpers that need nothing but the BCL (`BookmarkPayloadReader`, `BookmarkResumeValidator`).

It exists apart from `Elsa.Bookmarks.Ui` because that package depends on `Elsa`, `Elsa.Scheduling`,
`Elsa.Workflows.Core` and `Elsa.Workflows.Runtime`. A client that only *reads* a described bookmark - a
Blazor host, a mobile app - does not need the engine and should not have to take it.

Writing a provider, or having bookmarks described, means taking `Elsa.Bookmarks.Ui` instead; it
references this package and passes everything here through.
