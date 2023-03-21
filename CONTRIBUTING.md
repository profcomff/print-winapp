# Руководство по внесению изменений

## Структура кодовой базы

Общая структура файлов отталкивается от [паттерна MVVM](https://metanit.com/sharp/wpf/22.1.php) (Model-View-ViewModel) позволяет отделить логику приложения от визуальной части (представления). [Wikipedia Паттерн MVVM](https://ru.wikipedia.org/wiki/Model-View-ViewModel).

* Папка `Fonts` содержит используемые шрифты приложения в формате `.ttf`. Необходимо чтобы на любом компьютере шрифты были с приложением.
* Папка `Images` содержит используемые картинки. Растровый формат `.png` предпочтительней `.jpg` из за потерь последнего и не поддержке прозрачности.
* Файл `App.xaml`. Этот файл XAML определяет приложение WPF и все его ресурсы. Он также используется для определения пользовательского интерфейса, автоматически отображаемого при запуске приложения (в данном случае — MainWindow.xaml).
* Файл `App.xaml.cs`. Этот файл содержит логику создания и настройки нашего окна из `MainWindow.xaml` и обработку закрытие окна и приложения, также загрузку конфига приложения с помощью вызова `LoadConfig()` у класса `ConfigFile.cs`
* Файл `MainWindow.xaml`. View из паттерна MVVM. Этот файл XAML представляет главное окно приложения, в котором отображается созданное содержимое страниц. Класс Window определяет свойства окна, такие как заголовок, размер и значок.
* Файл `MainWindow.xaml.cs`. Этот файл содержит обработчики событий элементов окна (нажатия на кнопки, вставка в блок с кодом и т.д.)
* Файл `PrinterViewModel.cs`. ViewModel из паттерна MVVM. Этот файл реализует интерфейс `INotifyPropertyChanged` через класс `NotifyPropertyChangeBase.cs` и содержит поля, и свойства этих полей, которые используются для привязки к ним свойств графических элементов в `MainWindow.xaml`
* Файл `PrinterModel.cs`. Model из паттерна MVVM. Содержит основную логику программы и обновляет свойства в `PrinterViewModel.cs`. Содержит обработку нажатия на печать, проводит запросы к [print-api](https://github.com/profcomff/print-api) для валидации кода печати, скачивания файлов, отправки их `Sumatra PDF`, работу c QR.
* Файл `Marketing.cs`. Файл содержит методы для отправки сообщений на сервис [маркетинга](https://github.com/profcomff/marketing-api)

## Работа авто-обновления

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

Обновление можно запустить в ручном режиме, начиная с версии v2.0.11, введя в окно кода `UPDATE` и нажав `Alt+F4`.

## Версионирование

При подготовке новой версии программы следует изменять версию программы согласно [нумерации версии программного обеспечения](https://ru.wikipedia.org/wiki/%D0%9D%D1%83%D0%BC%D0%B5%D1%80%D0%B0%D1%86%D0%B8%D1%8F_%D0%B2%D0%B5%D1%80%D1%81%D0%B8%D0%B9_%D0%BF%D1%80%D0%BE%D0%B3%D1%80%D0%B0%D0%BC%D0%BC%D0%BD%D0%BE%D0%B3%D0%BE_%D0%BE%D0%B1%D0%B5%D1%81%D0%BF%D0%B5%D1%87%D0%B5%D0%BD%D0%B8%D1%8F#%D0%A3%D0%BA%D0%B0%D0%B7%D0%B0%D0%BD%D0%B8%D0%B5_%D1%81%D1%82%D0%B0%D0%B4%D0%B8%D0%B8_%D1%80%D0%B0%D0%B7%D1%80%D0%B0%D0%B1%D0%BE%D1%82%D0%BA%D0%B8) в файле `PrinterApp/PrinterApp.csproj` переменная `<Version>2.0.7.0</Version>`.

## Дополнительные ссылки

* [Руководство. Создание простого приложения WPF с помощью C #](https://learn.microsoft.com/ru-ru/visualstudio/get-started/csharp/tutorial-wpf?view=vs-2022)
* [Руководство: Создание первого приложения WPF в Visual Studio 2019](https://learn.microsoft.com/ru-ru/dotnet/desktop/wpf/getting-started/walkthrough-my-first-wpf-desktop-application?view=netframeworkdesktop-4.8)
* [Учебник. Создание приложения WPF с помощью .NET](https://learn.microsoft.com/ru-ru/dotnet/desktop/wpf/get-started/create-app-visual-studio?view=netdesktop-7.0)
* [HttpClient Класс](https://learn.microsoft.com/ru-ru/dotnet/api/system.net.http.httpclient?view=net-7.0)
* [ClientWebSocket Класс](https://learn.microsoft.com/ru-ru/dotnet/api/system.net.websockets.clientwebsocket?view=net-7.0)
