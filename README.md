# Терминал печати

![brutal-printer](https://user-images.githubusercontent.com/13213573/200373331-70c45e14-a81f-4069-8fcb-0a020ca89832.png)

[![Release](https://github.com/profcomff/print-winapp/actions/workflows/deploy-printer-app.yml/badge.svg)](https://github.com/profcomff/print-winapp/actions/workflows/deploy-printer-app.yml/badge.svg)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/profcomff/print-winapp)

Позволяет выводить файл на печать, после загрузки файла на [printer.ui.profcomff.com](https://printer.ui.profcomff.com/).  
Мотивация создания была в том что была необходима система, позволяющая ограничить пользователю доступ к операционной системе и сократить количество действий для получения напечатанного документа.

## Функционал

* Сокрытие доступа к операционной системе для пользователя.
* Передачу скачанных документов pdf на печать через [Sumatra PDF](https://www.sumatrapdfreader.org/download-free-pdf-viewer) с параметрами пользователя.
* Позволяет пользователю отправить файл на печать при помощи ввода кода документа.
* Позволяет пользователю отправить файл на печать при помощи  сканирования QR кода.
* После успешной печати выдает комплимент пользователю.
* Автоматическая смена дизайна на Новогодний период.
* Имеет функцию автоматического обновления программы.
* Имеет функцию автоматического обновления по запросу с сервера.
* Имеет функцию автоматической перезагрузки по запросу с сервера.

## Быстрый старт

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

Программа автоматически записывает историю своей работы в файл в папке `%userprofile%/.printerAppLogs/`.  
Путь для временного хранения файлов находится в `%temp%/.printerApp/`.

## Руководство по внесению изменений

Программа написана под Windows на .NET 8 с использованием технологии [Windows Presentation Foundation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/?view=netdesktop-8.0).  
Минимально для сборки проекта понадобится установленный [Microsoft .NET 8 SDK](https://dotnet.microsoft.com/en-us/download). Для графического редактирования интерфейсов рекомендуется использовать microsoft Visual Studio Blend 2022.  
[Продолжение в CONTRIBUTING.md](CONTRIBUTING.md)

![doom-bigfont-good-luck-newbie](https://user-images.githubusercontent.com/13213573/200591035-6a69a06e-21dd-4145-a492-4c78a36e750b.png)
