namespace Kh2JapaneseSystem

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI.Hosts
open Avalonia.Input
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.Shared.PlatformSupport

type MainWindow() as this =
    inherit HostWindow()
    
    let loader = AssetLoader()
    
    do
        this.Title <- "JapaneseEvent"
        this.Width <- 800.0
        this.Height <- 600.0
        
        Elmish.Program.mkSimple (fun () -> KhCodecs.init) KhCodecs.update KhCodecs.view
        |> Elmish.Program.withHost this
        |> Elmish.Program.withConsoleTrace
        |> Elmish.Program.run

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Load "avares://Avalonia.Themes.Fluent/FluentLight.xaml"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .With(Win32PlatformOptions(UseWgl = true))
            .StartWithClassicDesktopLifetime(args)
