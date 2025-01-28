dotnet publish .\RaggedBooks.MauiClient\RaggedBooks.MauiClient.csproj   `
    -f net9.0-windows10.0.19041.0 `
    -c Release `
    -p:RuntimeIdentifierOverride=win-x64 `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true `
    -o ../publishedapp


