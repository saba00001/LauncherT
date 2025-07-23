# HRP Launcher v2.0

HRP (High Quality Roleplay) სერვერის თანამედროვე ლაუნჩერი MTA:SA-სთვის.

## 🌟 ყველაზე მთავარი ფუნქციები

### 🎨 თანამედროვე დიზაინი
- **Material Design** და **Modern WPF** themes
- რეაქტიული UI ანიმაციებით
- **Dark Theme** მხარდაჭერით
- 4K/High DPI მონიტორების მხარდაჭერა
- მრავალფეროვანი გრადიენტები და ვიზუალური ეფექტები

### 🚀 გაუმჯობესებული ფუნქციონალი
- **Discord Rich Presence** ინტეგრაცია
- ავტომატური განახლების შემოწმება
- რეალურ დროში სერვერის სტატუსის მონიტორინგი
- თამაშის პროცესის მონიტორინგი
- System Tray მხარდაჭერა

### 🛡️ უსაფრთხოება და სტაბილურობა
- შეცდომების დამუშავება
- კონფიგურაციების ავტომატური backup
- Player name validation
- ქსელის timeout მართვა

### 📊 მონიტორინგი
- სერვერის პინგი და online status
- მოთამაშეების რაოდენობა
- ავტომატური reconnection
- ქსელის ხარისხის ინდიკატორები

## 📦 ინსტალაცია

### საჭირო მოთხოვნები
- Windows 10/11
- .NET 8.0 Runtime
- MTA:SA 1.5+
- Discord (Rich Presence-ისთვის, არასავალდებულო)

### ბილდი
```bash
# Repository-ს კლონირება
git clone https://github.com/your-repo/hrp-launcher.git
cd hrp-launcher

# დამოკიდებულებების აღდგენა
dotnet restore

# ბილდი
dotnet build --configuration Release

# გაშვება
dotnet run
```

### Release Build
```bash
# Single-file executable-ის შექმნა
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

## ⚙️ კონფიგურაცია

ლაუნჩერი ავტომატურად შექმნის კონფიგურაციის ფაილს:
```
%ProgramFiles%\HRP Launcher\launcher_config.json
```

### კონფიგურაციის პარამეტრები
```json
{
  "LastPlayerName": "YourPlayerName",
  "AutoCheckUpdates": true,
  "AutoCheckInterval": 300000,
  "EnableDiscordRichPresence": true,
  "MinimizeToTray": true,
  "StartWithWindows": false,
  "WindowWidth": 950,
  "WindowHeight": 650
}
```

## 🎮 გამოყენება

1. **Player Name**: შეიყვანეთ თქვენი სათამაშო სახელი
2. **Version Check**: ავტომატურად ამოწმებს განახლებებს
3. **Server Status**: აჩვენებს სერვერის მდგომარეობას
4. **Launch Game**: იწყებს თამაშს MTA:SA-ში

### Hot Keys
- `Ctrl + M`: Minimize to tray
- `Ctrl + R`: Refresh/Check updates
- `Ctrl + S`: Open settings
- `F5`: Force refresh server status

## 🔧 დეველოპერებისთვის

### პროექტის სტრუქტურა
```
HRP/
├── MainWindow.xaml          # მთავარი UI
├── MainWindow.xaml.cs       # მთავარი ლოგიკა
├── App.xaml                 # Application resources
├── App.xaml.cs              # Application startup
├── Assets/                  # Icons და images
└── HRP.csproj              # Project file
```

### NuGet Packages
- `DiscordRichPresence` - Discord ინტეგრაციისთვის
- `ModernWpfUI` - თანამედროვე UI
- `MaterialDesignThemes` - Material Design
- `Hardcodet.NotifyIcon.Wpf` - System Tray
- `Microsoft.Web.WebView2` - Web content

### API Endpoints
```
https://pub-e8ef73117d8c40baa7cb599b15297f19.r2.dev/version.txt    # Version info
https://pub-e8ef73117d8c40baa7cb599b15297f19.r2.dev/bin.zip       # Game files
https://pub-e8ef73117d8c40baa7cb599b15297f19.r2.dev/news.json     # News feed
```

## 🐛 Bug Reports

Issues-ის შესახებ GitHub-ზე რეპორტი გააკეთეთ:
1. OS ვერსია
2. .NET Runtime ვერსია
3. შეცდომის აღწერა
4. ლოგ ფაილები (Console output)

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📝 ცვლილებების ისტორია

### v2.0.0 (2024)
- 🎨 სრულიად ახალი UI დიზაინი
- 🚀 Discord Rich Presence
- 📊 Real-time server monitoring
- 🛡️ გაუმჯობესებული error handling
- ⚙️ Advanced configuration options
- 🔔 System tray notifications

### v1.0.0 (2023)
- 🎮 ბაზისური launcher functionality
- 📦 Automatic updates
- 🎯 MTA:SA ინტეგრაცია

## 📞 მხარდაჭერა

- **Discord**: [HRP Discord Server](https://discord.gg/hrp)
- **Website**: [hrp.ge](https://hrp.ge)
- **Email**: support@hrp.ge

## 📄 ლიცენზია

MIT License - იხილეთ [LICENSE](LICENSE) ფაილი დეტალებისთვის.

---

**HRP Team** © 2024 - Made with ❤️ for Georgian Gaming Community