$rgName = "rg-beautiful-tables"
$location = "swedencentral"
$appName = "mkbtfl-tbls-app"
$functionAppName = "$appName-func"
$projectPath = "../azfnct"

az group create --name $rgName --location $location

az deployment group create `
  --resource-group $rgName `
  --template-file main.bicep `
  --parameters main.bicepparam

# Publish and deploy the .NET project
dotnet publish $projectPath --configuration Release --output ./publish

Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

az functionapp deployment source config-zip `
  --resource-group $rgName `
  --name $functionAppName `
  --src ./publish.zip

Remove-Item -Path ./publish -Recurse -Force
Remove-Item -Path ./publish.zip -Force