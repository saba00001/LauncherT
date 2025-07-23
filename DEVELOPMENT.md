# HRP Launcher - Development Guide

ეს დოკუმენტი არის დეველოპერებისთვის, რომლებიც მუშაობენ HRP Launcher-ზე.

## 🔧 Development Environment Setup

### საჭირო მოთხოვნები
- **Visual Studio 2022** ან **Visual Studio Code** + C# Extension
- **.NET 8.0 SDK**
- **Git**
- **Windows 10/11** (WPF აპლიკაციისთვის)

### IDE Setup

#### Visual Studio 2022
1. დააინსტალირეთ **Visual Studio 2022** შემდეგი workloads-ით:
   - `.NET Desktop Development`
   - `Windows App SDK C# Templates`

#### Visual Studio Code
1. დააინსტალირეთ extensions:
   - C# for Visual Studio Code
   - .NET Install Tool
   - XAML
   - GitLens

### პროექტის Clone და Setup
```bash
git clone https://github.com/your-repo/hrp-launcher.git
cd hrp-launcher
dotnet restore
dotnet build
```

## 📁 პროექტის სტრუქტურა

```
HRP/
├── App.xaml                 # Application-level XAML
├── App.xaml.cs              # Application startup logic
├── MainWindow.xaml          # მთავარი ფანჯრის UI
├── MainWindow.xaml.cs       # მთავარი ფანჯრის logic
├── Assets/                  # Icons, images, resources
├── Properties/              # Assembly info
├── HRP.csproj              # Project configuration
├── build.bat               # Build script
├── README.md               # User documentation
├── DEVELOPMENT.md          # This file
├── LICENSE                 # MIT License
└── .gitignore             # Git ignore rules
```

## 🏗️ Architecture

### MVVM Pattern
პროექტი იყენებს მარტივ MVVM-ის მიდგომას:
- **View**: XAML ფაილები (MainWindow.xaml)
- **ViewModel/CodeBehind**: .xaml.cs ფაილები
- **Model**: Configuration classes (LauncherConfig)

### Key Components

#### MainWindow Class
მთავარი კლასი რომელიც მართავს:
- Discord Rich Presence
- Version checking
- Server status monitoring
- Game launching
- UI updates

#### Configuration System
```csharp
private class LauncherConfig
{
    public string LastPlayerName { get; set; }
    public bool EnableDiscordRichPresence { get; set; }
    public bool AutoCheckUpdates { get; set; }
    // ... მეტი პარამეტრები
}
```

#### Timer System
- `autoUpdateTimer`: ავტომატური განახლების შემოწმება
- `serverStatusTimer`: სერვერის სტატუსის მონიტორინგი
- `gameMonitorTimer`: თამაშის პროცესის მონიტორინგი

## 🎨 UI/UX Design Guidelines

### Color Scheme
```xaml
<!-- Primary Colors -->
<SolidColorBrush x:Key="PrimaryBrush" Color="#FF0078D4"/>
<SolidColorBrush x:Key="SecondaryBrush" Color="#FF00BCF2"/>
<SolidColorBrush x:Key="AccentBrush" Color="#FF40E0D0"/>

<!-- Background Colors -->
<SolidColorBrush x:Key="DarkBackgroundBrush" Color="#FF1E1E1E"/>
<SolidColorBrush x:Key="MediumBackgroundBrush" Color="#FF2D2D30"/>
<SolidColorBrush x:Key="LightBackgroundBrush" Color="#FF3F3F46"/>

<!-- Text Colors -->
<SolidColorBrush x:Key="TextPrimaryBrush" Color="#FFFFFFFF"/>
<SolidColorBrush x:Key="TextSecondaryBrush" Color="#FFCCCCCC"/>

<!-- Status Colors -->
<SolidColorBrush x:Key="SuccessBrush" Color="#FF00FF41"/>
<SolidColorBrush x:Key="WarningBrush" Color="#FFFF8C00"/>
<SolidColorBrush x:Key="ErrorBrush" Color="#FFFF4444"/>
```

### Typography
- **Header**: Font Size 54, Bold
- **Subheader**: Font Size 18, Medium
- **Body**: Font Size 14, Regular
- **Caption**: Font Size 12, Light

### Animations
- **FadeIn**: Opacity 0→1, Duration 0.5s
- **SlideIn**: TranslateY 50→0, Duration 0.6s
- **Hover Effects**: Shadow და color transitions

## 🔨 Building

### Debug Build
```bash
dotnet build --configuration Debug
```

### Release Build
```bash
dotnet build --configuration Release
```

### Single-File Executable
```bash
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

### Using Build Script
```cmd
build.bat
```

## 🧪 Testing

### Manual Testing Checklist
- [ ] ლაუნჩერი იხსნება without errors
- [ ] Player name validation მუშაობს
- [ ] Version checking მუშაობს
- [ ] Server status checking მუშაობს
- [ ] Discord Rich Presence მუშაობს
- [ ] Game launching მუშაობს
- [ ] System tray functionality მუშაობს
- [ ] Configuration saving/loading მუშაობს
- [ ] Error handling მუშაობს properly

### Error Testing
- Disconnect internet და test offline behavior
- Invalid player names
- MTA:SA not installed
- Discord not running
- Server unavailable

## 📝 Code Style

### C# Conventions
- **PascalCase**: Classes, Methods, Properties
- **camelCase**: Fields, local variables
- **Private fields**: `_fieldName` ან `fieldName`
- **Constants**: `UPPER_CASE`

### XAML Conventions
- **PascalCase**: Element names
- **camelCase**: Event handlers
- Indentation: 4 spaces
- Close tags on new lines for complex elements

### Comments
```csharp
#region Region Name
// Single line comments for simple explanations
/// <summary>
/// XML documentation for public APIs
/// </summary>
/* Multi-line comments for complex logic */
#endregion
```

## 🚀 Deployment

### Release Process
1. Update version in `HRP.csproj`
2. Update CHANGELOG.md
3. Test thoroughly
4. Create release build
5. Test release build
6. Create GitHub release
7. Upload executable
8. Update download links

### Distribution
- Single `.exe` file
- No installer needed
- Self-contained: false (requires .NET Runtime)
- Target: `win-x64`

## 🐛 Debugging

### Common Issues

#### Discord RPC Not Working
```csharp
// Check if Discord is running
// Verify Discord Client ID
// Check network connectivity
```

#### Game Not Launching
```csharp
// Verify MTA:SA installation path
// Check command line arguments
// Verify file permissions
```

#### UI Not Responsive
```csharp
// Use async/await for long operations
// Update UI on Dispatcher thread
// Avoid blocking UI thread
```

### Logging
```csharp
private void LogInfo(string message)
{
    Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
}

private void LogError(string message)
{
    Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
}
```

## 🔄 Version Control

### Branch Strategy
- `main`: Stable releases
- `develop`: Development branch
- `feature/*`: New features
- `hotfix/*`: Critical fixes

### Commit Messages
```
type(scope): description

feat(ui): add new button animation
fix(discord): resolve connection timeout
docs(readme): update installation guide
style(xaml): fix indentation
refactor(config): simplify settings structure
test(launch): add game launching tests
```

## 📚 Resources

### Documentation
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [Material Design](https://material.io/design)
- [Discord Rich Presence](https://discord.com/developers/docs/rich-presence/how-to)

### Tools
- [XAML Styler](https://github.com/Xavalon/XamlStyler) - XAML formatting
- [ILSpy](https://github.com/icsharpcode/ILSpy) - .NET decompiler
- [Process Monitor](https://docs.microsoft.com/en-us/sysinternals/downloads/procmon) - File/Registry monitoring

---

**Happy Coding! 🚀**