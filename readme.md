 <div align="center">

# Organize my Downloads

__The quick sorter app that moves files from Downloads to system folders, with options to rename files, add date & time, add a random number (1–50), or combine all options at once.__

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?logo=windows)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
[![C#](https://custom-icon-badges.demolab.com/badge/C%23-%23239120.svg?logo=cshrp&logoColor=white)](#)
[![Windows](https://custom-icon-badges.demolab.com/badge/Windows-0078D6?logo=windows11&logoColor=white)](#)
[![ChatGPT](https://custom-icon-badges.demolab.com/badge/ChatGPT-74aa9c?logo=openai&logoColor=white)](#)

</div>


### __Englsih__ | [Русский](#englsih--русский-1)
## Quick Start

1. Open [Releases](https://github.com/NikOneTGG/organizer-for-downloads/releases) page
2. Download the latest `DownloadManager.exe`
3. Run the app


## Auto Tests

```
dotnet test .\DownloadManager.slnx -c Release
```

Latest result:

- __35 passed__
- __0 failed__
- __0 skipped__



## Stack

| <!-- -->      | <!-- -->    |
|:-------------:|:-----------:|
| __lang__      | __C#__      |
| __framework__ | __.NET 10__ |
| __interface__ | __WPF__     |
|__integration__| __WinAPI__  |
| __Tests__     | __xUnit__   |



## Structure

```
organizer-for-downloads/
├──src/
│  ├──DownloadManager/
│  │  ├──assets/
│  │  ├──App.xaml
│  │  └──DownloadManager.csproj
│  └──DownloadManager.Core/
│       ├──AppSettings.cs
│       ├──program.cs
│       ├──renamer.cs
│       └──DownloadManager.Core.csproj
├──tests/
│  └──DownloadManager.Tests/
├──DownloadManager.slnx
├──LICENSE
└──README.md
```

## Build from source

```
git clone https://github.com/NikOneTGG/organizer-for-downloads.git
cd organizer-for-downloads
dotnet build .\DownloadManager.slnx -c Release
```

Run tests:

```
dotnet test .\DownloadManager.slnx -c Release
```

## AI used for

__This is my training project so i used AI for search info and searching bugs in the code, but i didn't used AI to code__


## License

This project is licensed under the [MIT License](LICENSE).



### [Englsih](#englsih--русский) | __Русский__ 

# Organize my Downloads

__Приложение занимающееся автосортировкой файлов по разным системным папкам с возможностью переименовки/добавления даты/случайного числа от 1 до 50, ну или всего сразу__

## Запуск

1. Открыть страницу [Релизов](https://github.com/NikOneTGG/organizer-for-downloads/releases)
2. Скачать последнюю версию `DownloadManager.exe`
3. Запустить

Готово! Установка не требуется. Отдельно устанавливать .NET Runtime тоже не нужно.


## Автотесты

```
dotnet test .\DownloadManager.slnx -c Release
```

Последний результат:

- __35 успешно пройдено__
- __0 ошибок__
- __0 пропущено__



## Стек

| <!-- -->          | <!-- -->    |
|:-----------------:|:-----------:|
| __ЯП__            | __C#__      |
| __фреймворк__     | __.NET 10__ |
| __интерфейс__     | __WPF__     |
|__интеграция с ОС__| __WinAPI__  |
| __Автотесты__     | __xUnit__   |



## Структура

```
organizer-for-downloads/
├──src/
│  ├──DownloadManager/
│  │  ├──assets/
│  │  ├──App.xaml
│  │  └──DownloadManager.csproj
│  └──DownloadManager.Core/
│       ├──AppSettings.cs
│       ├──program.cs
│       ├──renamer.cs
│       └──DownloadManager.Core.csproj
├──tests/
│  └──DownloadManager.Tests/
├──DownloadManager.slnx
├──LICENSE
└──README.md
```



## Cборка из исходника


```
git clone https://github.com/NikOneTGG/organizer-for-downloads.git
cd organizer-for-downloads
dotnet build .\DownloadManager.slnx -c Release
```

Запуск тестов:

```
dotnet test .\DownloadManager.slnx -c Release
```

## Для чего использовался ИИ?

__Изначально я делал этот проект как учебный и, соответственно первый на C# дабы познать основы взаимодействия с системой, ИИ я использовал исключительно для поиска информации и поиска багов в коде__

## Лицензия

Проект распространяется под лицензией [MIT](LICENSE).