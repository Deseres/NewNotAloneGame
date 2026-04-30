# Monolith Deployment Guide - NotAlone Game

## Overview
This guide explains how to deploy your .NET backend and React frontend as a single monolith to Azure App Service.

---

## Architecture

```
┌─────────────────────────────────────────────┐
│        Azure App Service (Single)           │
├─────────────────────────────────────────────┤
│  .NET Backend (NotAlone.dll)                │
│  ├── API Routes (/api/*)                    │
│  ├── Static Files Handler                   │
│  └── SPA Fallback (index.html)              │
│                                              │
│  Static Files (wwwroot/)                    │
│  ├── index.html                             │
│  ├── *.js, *.css, *.assets                  │
│  └── Frontend App (React)                   │
└─────────────────────────────────────────────┘
```

---

## What Changed

### 1. **Program.cs** - Updated to Serve Static Files
Added middleware to serve frontend from `wwwroot`:
```csharp
// Serve static files (frontend build output)
app.UseDefaultFiles();      // Serves index.html by default
app.UseStaticFiles();       // Serves other static assets

// SPA fallback for client-side routing
app.MapFallbackToFile("index.html");
```

### 2. **Frontend Build Configuration**
- Vite builds to `Frontend/dist`
- Dist folder is copied to backend's `wwwroot` during monolith build
- Frontend uses relative paths (`/api/*`) for API calls

---

## Local Build & Test

### Build Monolith Locally
```powershell
# Run the build script
.\build-monolith.ps1 -Configuration Release

# Output: ./bin/Release/net10.0/publish/
# This folder contains everything needed for deployment
```

### Test Locally
```powershell
# Run the backend (serves frontend + API)
dotnet run

# Frontend available at: http://localhost:5000
# API available at: http://localhost:5000/api/*
```

---

## Azure Deployment Steps

### **Option 1: Manual Deployment (Using Azure CLI)**

#### Prerequisites
```powershell
# Install Azure CLI if not already installed
# Download from: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli

# Login to Azure
az login

# List your subscriptions
az account list --output table

# Set your subscription
az account set --subscription "Subscription Name or ID"
```

#### Deploy
```powershell
# Build monolith
.\build-monolith.ps1 -Configuration Release

# Navigate to publish folder
cd .\bin\Release\net10.0\publish

# Create ZIP file
$zipPath = ".\..\..\..\..\monolith.zip"
Compress-Archive -Path * -DestinationPath $zipPath -Force

# Navigate back
cd .\..\..\..\..\

# Deploy to Azure App Service
$resourceGroup = "your-resource-group"
$appServiceName = "your-app-service-name"

az webapp deployment source config-zip `
  --resource-group $resourceGroup `
  --name $appServiceName `
  --src-path monolith.zip

# Monitor deployment
az webapp log tail --resource-group $resourceGroup --name $appServiceName
```

### **Option 2: GitHub Actions (Recommended)**

1. **Add GitHub Secrets**
   - Go to your GitHub repo → Settings → Secrets and variables → Actions
   - Add `AZURE_PUBLISH_PROFILE`:
     ```
     1. Go to Azure Portal
     2. Navigate to your App Service
     3. Click "Download publish profile"
     4. Copy contents and add as secret in GitHub
     ```

2. **Workflow File is Already Created**
   - Location: `.github/workflows/deploy.yml`
   - Triggers automatically on `git push` to `main` branch
   - Builds frontend → Builds backend → Deploys to Azure

3. **Deploy**
   ```bash
   git add .
   git commit -m "Add monolith deployment"
   git push origin main
   # GitHub Actions automatically deploys to Azure
   ```

### **Option 3: Azure App Service Deployment Center**

1. **In Azure Portal:**
   - Go to your App Service → Deployment Center
   - Select: GitHub
   - Connect your repository
   - Set build provider: GitHub Actions
   - Configure:
     - Branch: `main`
     - Build preset: `.NET`

2. **Monitor:**
   - Deployment history shown in portal
   - View logs in GitHub Actions tab

---

## API Endpoints After Deployment

```
Frontend:        https://your-app.azurewebsites.net
API:             https://your-app.azurewebsites.net/api
Swagger:         https://your-app.azurewebsites.net/swagger
Health Check:    https://your-app.azurewebsites.net (returns frontend)
```

---

## Frontend Configuration Update

Since frontend and backend are now on same domain, you should update API endpoints:

**Before (separate domains):**
```typescript
const API_BASE = "https://notalone-api-bjbza7e9gafjfrfv.swedencentral-01.azurewebsites.net/api"
```

**After (monolith):**
```typescript
const API_BASE = "/api"  // Relative path
```

This ensures:
- Development: Works with Vite proxy (http://localhost:5173 → http://localhost:5000/api)
- Production: Works with served static files (https://your-app.azurewebsites.net → /api)

---

## Verify Deployment

```powershell
# Test frontend is served
Invoke-WebRequest https://your-app.azurewebsites.net -UseBasicParsing

# Test API is accessible
Invoke-WebRequest https://your-app.azurewebsites.net/api/auth/login `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body '{"email":"test@example.com","password":"test"}'

# View logs
az webapp log tail --resource-group $resourceGroup --name $appServiceName
```

---

## Troubleshooting

### "Frontend not loading (404 on index.html)"
- Ensure `MapFallbackToFile("index.html")` is in Program.cs
- Verify wwwroot folder exists in publish output
- Check: `https://your-app.azurewebsites.net/index.html`

### "API returns 404"
- Verify controllers are mapped: `app.MapControllers()`
- Check endpoint format: `/api/auth/login` (not `/api//auth/login`)
- Test with Swagger: `https://your-app.azurewebsites.net/swagger`

### "CORS errors"
- Monolith uses same origin, CORS not needed for frontend
- Keep `AllowAll` policy for external API consumers

### "Build fails in GitHub Actions"
- Check Node.js version (should be 18+)
- Check .NET version (should be 10.0)
- Review GitHub Actions logs for exact error

---

## File Structure After Monolith Build

```
bin/Release/net10.0/publish/
├── wwwroot/                  ← Frontend static files
│   ├── index.html
│   ├── assets/
│   ├── *.js
│   └── *.css
├── NotAlone.dll
├── NotAlone.runtimeconfig.json
├── appsettings.json
├── appsettings.Production.json
└── [other .NET assemblies]
```

---

## Next Steps

1. ✅ Update Program.cs (DONE)
2. ✅ Update Vite config (DONE)
3. ✅ Create build script (DONE)
4. ✅ Create GitHub Actions workflow (DONE)
5. [ ] Update frontend API endpoints to use `/api` relative paths
6. [ ] Test locally: `.\build-monolith.ps1` then `dotnet run`
7. [ ] Deploy to Azure using one of the methods above
8. [ ] Test production endpoints

---

## Performance Tips

- Gzip compression is automatic in ASP.NET Core
- Consider Azure CDN for static assets if needed
- Frontend is served directly from memory (no cold start)
- API and frontend share connection pool to database

---

## Rollback

If deployment fails:
```powershell
# Swap slots (if using deployment slots)
az webapp deployment slot swap `
  --resource-group $resourceGroup `
  --name $appServiceName `
  --slot staging

# Or redeploy previous version from GitHub Actions
# Go to Actions → Select previous successful run → Deploy
```

---

## Additional Resources

- [ASP.NET Core Hosting SPAs](https://learn.microsoft.com/en-us/aspnet/core/spa/introduction)
- [Azure App Service Deployment](https://learn.microsoft.com/en-us/azure/app-service/deploy-github-actions)
- [GitHub Actions for .NET](https://github.com/actions/setup-dotnet)
