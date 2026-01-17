# GitHub Actions - Automated Build Guide

## What Just Happened?

GitHub Actions is now configured to **automatically build your application** every time you push code!

---

## How It Works

### 1. **Automatic Builds on Every Push**

```bash
# You make changes in Visual Studio
git add .
git commit -m "Fixed equipment tagging bug"
git push

# GitHub automatically (in 5-10 minutes):
# ✓ Builds the entire solution
# ✓ Publishes the UI application
# ✓ Compiles the AutoCAD plugin
# ✓ Creates the installer
# ✓ Uploads it for download
```

### 2. **Where to Find Your Builds**

1. Go to: https://github.com/sekkipras/pidstandard/actions
2. Click on the latest workflow run
3. Scroll down to **Artifacts**
4. Download **PIDStandardization-Build.zip**
5. Extract and find `PIDStandardization_Setup_v1.0.0.exe`

**Artifacts are kept for 30 days** - plenty of time to test and distribute!

---

## Creating Official Releases (For Your Team)

When you're ready to release a new version to your team:

### Step 1: Update Version Numbers

Update version in these files:
- `Deployment_Package/PIDStandardization_Installer.iss` (line 7)
- `PIDStandardization.UI/MainWindow.xaml.cs` (About dialog)

### Step 2: Commit and Tag

```bash
# Commit version changes
git add .
git commit -m "Bump version to 1.1.0"

# Create version tag
git tag v1.1.0

# Push with tags
git push origin main --tags
```

### Step 3: GitHub Automatically Creates Release!

GitHub will:
- ✓ Build the application
- ✓ Create the installer
- ✓ **Create a GitHub Release** at https://github.com/sekkipras/pidstandard/releases
- ✓ Upload the installer automatically
- ✓ Generate release notes from commits

Your team can now download from the Releases page!

---

## Workflow Triggers

The build runs automatically when:

1. **Push to main branch** - Every code push
2. **Pull request to main** - Test before merging
3. **Version tag push** - Creates official release (e.g., v1.0.0, v1.1.0)
4. **Manual trigger** - Go to Actions → Build and Release → Run workflow

---

## Monitoring Builds

### Check Build Status

1. Go to: https://github.com/sekkipras/pidstandard/actions
2. See all workflow runs with status:
   - ✓ Green checkmark = Success
   - ✗ Red X = Failed
   - 🟡 Yellow circle = In progress

### If a Build Fails

1. Click on the failed workflow run
2. Click on the "build" job
3. Expand the failed step to see error details
4. Fix the issue locally
5. Push the fix - GitHub rebuilds automatically

---

## Development Workflow Examples

### Quick Bug Fix

```bash
# Fix the bug in Visual Studio
git add .
git commit -m "Fix: Equipment save error on empty tag"
git push

# Wait 5-10 minutes
# Download from GitHub Actions → Artifacts
# Test the installer
```

### Feature Development

```bash
# Create feature branch
git checkout -b feature/excel-export

# Make changes, push to test
git push origin feature/excel-export

# GitHub builds it automatically!
# Download and test

# When ready, merge to main
git checkout main
git merge feature/excel-export
git push

# GitHub builds the main version
```

### Creating a Release

```bash
# Update version to 1.1.0 in files
git add .
git commit -m "Release v1.1.0 - Added Excel export"
git tag v1.1.0
git push origin main --tags

# GitHub automatically:
# - Builds installer
# - Creates release at /releases
# - Team can download immediately!
```

---

## What Gets Built

Every successful workflow creates:

1. **PIDStandardization.UI.exe** (170MB single-file application)
2. **AutoCAD Plugin DLLs** (All dependencies)
3. **PIDStandardization_Setup_v1.0.0.exe** (Professional installer)

All packaged and ready to distribute!

---

## Cost

**GitHub Actions is FREE for public repositories!**

For private repositories (like yours):
- Free tier: 2,000 minutes/month
- Your builds take ~5-10 minutes each
- You can do ~200-400 builds/month for free
- More than enough for normal development

---

## Advantages Over Local Building

| Local Build | GitHub Actions |
|-------------|----------------|
| Takes up your computer resources | Runs on GitHub's servers |
| Must wait for build to finish | Build while you keep working |
| Only on your computer | Available to whole team |
| Manual process | Fully automated |
| No history | Complete build history |
| One at a time | Can test multiple branches |

---

## Tips

1. **Test locally first** - Don't rely on GitHub for every small change
2. **Use branches** - Test features before merging to main
3. **Tag releases** - Use v1.0.0, v1.1.0 format for auto-releases
4. **Check Actions tab** - Monitor build status regularly
5. **Download artifacts** - Test before creating official releases

---

## Troubleshooting

### Build is taking too long (>15 minutes)

- Normal build time: 5-10 minutes
- Check Actions tab for progress
- If stuck, cancel and re-run

### Build failed with "Restore failed"

- NuGet packages issue
- Usually fixes itself on retry
- Check internet connectivity on GitHub's side

### Installer not created

- Check Inno Setup step in workflow log
- Verify PIDStandardization_Installer.iss syntax
- Test locally with BUILD_AND_DEPLOY.bat first

### Can't find artifacts

- Artifacts appear only after **successful** build
- Check if build completed (green checkmark)
- Artifacts expire after 30 days

---

## Next Steps

1. ✅ **GitHub Actions is now active!** Check: https://github.com/sekkipras/pidstandard/actions
2. ✅ **First build should be running** (triggered by the workflow file push)
3. ✅ **Wait 5-10 minutes** for the build to complete
4. ✅ **Download the artifact** to test
5. ✅ **When ready, create your first release** using the tag method above

**You'll never need to run BUILD_AND_DEPLOY.bat manually again!**
