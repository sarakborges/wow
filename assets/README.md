# Asset directory

Commit only project-authored assets and files whose licenses explicitly permit redistribution.

Keep local source installations and generated imports in:

- `assets/local/` — local source pointers or small user-maintained configuration; never copy a full game installation here.
- `assets/imported/` — generated conversion output; excluded from Git.

The repository ignores proprietary source and common extracted WoW asset extensions. Do not bypass those exclusions by renaming files or packaging extracted data.
