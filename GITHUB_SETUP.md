# GitHub Setup Guide

## First-Time Setup (Do this once)

### Step 1: Initialize Git Repository

Open Command Prompt or PowerShell in the project folder:

```bash
cd D:\Projects\PIDStandardization

# Initialize git
git init

# Add all files
git add .

# Create first commit
git commit -m "Initial commit: P&ID Standardization Application v1.0.0"
```

### Step 2: Create GitHub Repository

1. **Go to** https://github.com
2. **Sign in** to your account
3. **Click** the "+" icon → "New repository"
4. **Fill in**:
   - Repository name: `PIDStandardization`
   - Description: `P&ID Equipment Tagging and Standardization Application with AutoCAD Integration`
   - Visibility: `Private` (recommended) or `Public`
5. **Do NOT** initialize with README (we already have one)
6. **Click** "Create repository"

### Step 3: Connect Local Repository to GitHub

GitHub will show you commands. Use these:

```bash
# Add GitHub as remote
git remote add origin https://github.com/YOUR_USERNAME/PIDStandardization.git

# Push to GitHub
git branch -M main
git push -u origin main
```

**Replace** `YOUR_USERNAME` with your actual GitHub username!

---

## Daily Development Workflow

### Making Changes and Updating GitHub

```bash
# 1. Make sure you're on the main branch
git checkout main

# 2. Pull latest changes (if working with a team)
git pull origin main

# 3. Create a new branch for your feature
git checkout -b feature/your-feature-name

# 4. Make your changes in Visual Studio
# ... edit files, add features, test ...

# 5. Check what files changed
git status

# 6. Add your changes
git add .

# 7. Commit with a descriptive message
git commit -m "Add Excel export feature for equipment lists"

# 8. Push to GitHub
git push origin feature/your-feature-name

# 9. Go to GitHub and create a Pull Request
# Review and merge when ready
```

### Updating the Main Version

After merging your feature:

```bash
# Switch back to main
git checkout main

# Pull the latest (includes your merged feature)
git pull origin main

# Build and deploy
BUILD_AND_DEPLOY.bat
```

---

## Creating a Release

### When You're Ready to Deploy to Team

```bash
# 1. Update version number (see DEVELOPER_GUIDE.md)

# 2. Commit version bump
git add .
git commit -m "Bump version to 1.1.0"

# 3. Create a tag
git tag -a v1.1.0 -m "Version 1.1.0 - Added Excel export"

# 4. Push with tags
git push origin main --tags

# 5. Build the installer
BUILD_AND_DEPLOY.bat

# 6. Go to GitHub → Releases → "Create new release"
#    - Choose tag: v1.1.0
#    - Upload: PIDStandardization_Setup_v1.1.0.exe
#    - Write release notes
#    - Publish!
```

---

## Useful Git Commands

### Checking Status

```bash
git status          # See what files changed
git log             # See commit history
git diff            # See line-by-line changes
```

### Undoing Changes

```bash
git checkout -- filename.cs     # Discard changes to a file
git reset --hard                # Discard ALL uncommitted changes (careful!)
git revert commit-hash          # Undo a specific commit
```

### Branch Management

```bash
git branch                      # List branches
git branch -a                   # List all branches (including remote)
git checkout branch-name        # Switch to a branch
git branch -d branch-name       # Delete a branch (after merging)
```

### Pulling Updates

```bash
git pull origin main            # Get latest from GitHub
```

---

## Team Collaboration

### If Working with Multiple Developers

**Before starting work each day:**

```bash
git checkout main
git pull origin main
git checkout -b feature/your-new-feature
```

**When someone else makes changes:**

```bash
git checkout main
git pull origin main
# Now you have the latest code!
```

**Resolving conflicts:**

If Git says there's a conflict:
1. Open the conflicted file in Visual Studio
2. Look for `<<<<<<<`, `=======`, `>>>>>>>` markers
3. Choose which code to keep
4. Remove the conflict markers
5. Save, commit, and push

---

## GitHub Features to Use

### Issues

- Track bugs and feature requests
- Create an issue for each task
- Reference issues in commits: `git commit -m "Fix #5: Equipment save error"`

### Pull Requests

- Review code before merging to main
- Discuss changes with team
- Keep main branch stable

### Projects

- Create a project board for task tracking
- Organize features into milestones

### Wiki

- Document architecture decisions
- Keep notes for team

---

## Best Practices

### DO:
- ✅ Commit often with clear messages
- ✅ Create branches for features
- ✅ Pull before starting new work
- ✅ Test before pushing
- ✅ Write meaningful commit messages

### DON'T:
- ❌ Commit directly to main (use branches)
- ❌ Push broken code
- ❌ Commit large binary files (except releases)
- ❌ Use vague commit messages ("fix", "update")
- ❌ Forget to pull before starting work

---

## Quick Reference Card

| Task | Command |
|------|---------|
| See changes | `git status` |
| Stage all changes | `git add .` |
| Commit | `git commit -m "message"` |
| Push to GitHub | `git push origin branch-name` |
| Pull from GitHub | `git pull origin main` |
| Create branch | `git checkout -b feature/name` |
| Switch branch | `git checkout branch-name` |
| See history | `git log --oneline` |

---

## Troubleshooting

### "Authentication failed"

You need to use a Personal Access Token:
1. Go to GitHub → Settings → Developer settings → Personal access tokens
2. Generate new token (classic)
3. Use token as password when pushing

### "Your branch is behind"

```bash
git pull origin main
```

### "Merge conflict"

1. Open conflicted files
2. Resolve conflicts manually
3. `git add .`
4. `git commit`

---

## Next Steps

1. ✅ Push your code to GitHub
2. ✅ Invite team members as collaborators
3. ✅ Set up branch protection rules (optional)
4. ✅ Create your first issue
5. ✅ Make your first feature branch

**Your code is now safely backed up and ready for collaboration!**
