# Терминал печати

![brutal-printer](https://user-images.githubusercontent.com/13213573/200373331-70c45e14-a81f-4069-8fcb-0a020ca89832.png)

[![Release](https://github.com/profcomff/print-winapp/actions/workflows/deploy-printer-app.yml/badge.svg)](https://github.com/profcomff/print-winapp/actions/workflows/deploy-printer-app.yml/badge.svg)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/profcomff/print-winapp)

Позволяет выводить файл на печать, после загрузки файла на [printer.ui.profcomff.com](https://printer.ui.profcomff.com/)

## Использование

### Зависимости

* Windows 10 и старше.
* Для работы программы требуется наличие установленной программы просмотра PDF файлов [Sumatra PDF](https://www.sumatrapdfreader.org/download-free-pdf-viewer) (по стандартному ее пути установки или переносимой версии по пути `<терминал печати>/SumatraPDF/SumatraPDF.exe`).

### Установка

* Скачайте последний архив с [выпуском](https://github.com/profcomff/print-winapp/releases/latest) программы.
* Распакуйте архив (рекомендуется использовать путь `%localappdata%/PrinterWinApp`).
* Запустите `PrinterApp.exe` в первый раз, затем появится файл настроек `PrinterApp.json`.

Пример файла настроек `PrinterApp.json`:

```json
{
  "ExitCode": "dyakov",
  "TempSavePath": "C:\\Users\\dyakov\\AppData\\Local\\Temp\\.printerApp",
  "StartWithWindows": false,
  "AutoUpdate": true
}
```

### Дополнительно

Программа автоматически записывает историю свой работы в файл в папку `%userprofile%/.printerAppLogs/`.  
Путь для временного хранения файлов находится `%temp%/.printerApp/`.

## Руководство по внесению изменений

Программа написана под Windows на .NET 6 с использованием технологии [Windows Presentation Foundation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/?view=netdesktop-6.0).  
Минимально для сборки проекта понадобится установленный [Microsoft .NET 6 SDK](https://dotnet.microsoft.com/en-us/download). Для графического редактирования интерфейсов рекомендуется использовать microsoft Visual Studio Blend 2022.

### Работа авто-обновления

За авто-обновления отвечает класс `AutoUpdater.cs`.  
Принцип проверки новых версий заключается в том что каждый час сверяется текущее время системы на соответствие промежутку времени с 22:00 до 06:00. Если в него попадаем то делаем запрос на `https://github.com/profcomff/print-winapp/releases/latest` и в ответ получаем ссылку на последний выпуск `https://github.com/profcomff/print-winapp/releases/tag/v2.0.7`. Номер версии полученной от гитхаба дополняем если требуется до шаблона `число.число.число.число` сверяем с текущим номером версии программы

```c#
string githubVersion = "2.0.7.0";
string currentVersion = "2.0.6.0";
if (githubVersion != currentVersion)
{
    //DO download
}
```

Если они отличаются то скачиваем новый архив `PrinterApp_x86.zip` пример запроса `https://github.com/profcomff/print-winapp/releases/download/v2.0.7/PrinterApp_x86.zip` и запускаем обновление. Распаковка архива происходит стандартными средствами windows. Подход в простом сравнении позволяет делать как обновление до новой версии, так и автоматический откат к старой версии.
**Для автоматической сборки и публикации нового выпуска** на github необходимо создать метку вида `v*` например `v2.0.7` (Не стоит создавать релизы иным способом).

### Версионирование

При подготовке новой версии программы следует изменять версию программы согласно [нумерации версии программного обеспечения](https://ru.wikipedia.org/wiki/%D0%9D%D1%83%D0%BC%D0%B5%D1%80%D0%B0%D1%86%D0%B8%D1%8F_%D0%B2%D0%B5%D1%80%D1%81%D0%B8%D0%B9_%D0%BF%D1%80%D0%BE%D0%B3%D1%80%D0%B0%D0%BC%D0%BC%D0%BD%D0%BE%D0%B3%D0%BE_%D0%BE%D0%B1%D0%B5%D1%81%D0%BF%D0%B5%D1%87%D0%B5%D0%BD%D0%B8%D1%8F#%D0%A3%D0%BA%D0%B0%D0%B7%D0%B0%D0%BD%D0%B8%D0%B5_%D1%81%D1%82%D0%B0%D0%B4%D0%B8%D0%B8_%D1%80%D0%B0%D0%B7%D1%80%D0%B0%D0%B1%D0%BE%D1%82%D0%BA%D0%B8) в файле `PrinterApp/PrinterApp.csproj` переменная `<Version>2.0.7.0</Version>`.

![doom-bigfont-good-luck-newbie](https://user-images.githubusercontent.com/13213573/200591035-6a69a06e-21dd-4145-a492-4c78a36e750b.png)
