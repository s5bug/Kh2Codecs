namespace Kh2JapaneseSystem

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open OpenKh.Kh2.Messages
open OpenKh.Kh2.Messages.Internals
open System.Globalization

module KhCodecs =
    type State = { inputText: string ; jaSystem: string ; jaEvent: string ; intlSystem: string }
    let init = { inputText = "" ; jaSystem = "" ; jaEvent = "" ; intlSystem = "" }
    
    let jaSystemEncoder = JapaneseSystemEncode()
    let jaSystemDecoder = JapaneseSystemDecode()
    
    let jaEventEncoder = JapaneseEventEncode()
    let jaEventDecoder = JapaneseEventDecode()
    
    let intlSystemEncoder = InternationalSystemEncode()
    let intlSystemDecoder = InternationalSystemDecode()
    
    type Msg =
        | UpdateInput of newText : string
        | UpdateJaSystem of newText : string
        | UpdateJaEvent of newText : string
        | UpdateIntlSystem of newText : string
    
    let toByteString (encoder : IMessageEncode) (text : string) : string =
        let textCommand = MessageCommandModel(Command = MessageCommand.PrintText, Text = text)
        [textCommand]
        |> ResizeArray
        |> encoder.Encode
        |> Seq.toList
        |> List.map (fun (b : byte) -> System.String.Format("{0:X2}", b))
        |> String.concat " "
    
    let fromByteString (decoder : IMessageDecode) (text : string) : string =
        let splitOnSpaces = text.Split(" ")
        let toBytes = splitOnSpaces |> Array.map (fun str -> System.Byte.Parse (str, NumberStyles.HexNumber)) |> Seq.toList
        let decode = toBytes |> List.toArray |> decoder.Decode |> Seq.toList
        decode |> List.map (fun x -> x.Text) |> String.concat ""
    
    let rec update (msg : Msg) (state : State) : State =
        match msg with
        | UpdateInput text ->
            {
                inputText = text
                jaSystem = try toByteString jaSystemEncoder text with | _ -> state.jaSystem
                jaEvent = try toByteString jaEventEncoder text with | _ -> state.jaEvent
                intlSystem = try toByteString intlSystemEncoder text with | _ -> state.intlSystem
            }
        | UpdateJaSystem text ->
            update (UpdateInput (fromByteString jaSystemDecoder text)) state
        | UpdateJaEvent text ->
            update (UpdateInput (fromByteString jaEventDecoder text)) state
        | UpdateIntlSystem text ->
            update (UpdateInput (fromByteString intlSystemDecoder text)) state
    
    let inputBox (state : State) (dispatch) =
        WrapPanel.create [
            WrapPanel.children [
                TextBlock.create [
                    TextBlock.text "Input:"
                ]
                TextBox.create [
                    TextBox.watermark "Text"
                    TextBox.text state.inputText
                    TextBox.fontFamily "MS UI Gothic"
                    TextBox.onTextChanged (fun e -> dispatch (UpdateInput e))
                ]
            ]
        ]
    
    let jaSystemBox (state : State) (dispatch) =
        WrapPanel.create [
            WrapPanel.children [
                TextBlock.create [
                    TextBlock.text "Output (JA System):"
                ]
                TextBox.create [
                    TextBox.watermark "2f 46 41 32 40"
                    TextBox.text state.jaSystem
                    TextBox.onTextChanged (fun e -> dispatch (UpdateJaSystem e))
                ]
            ]
        ]
    
    let jaEventBox (state : State) (dispatch) =
        WrapPanel.create [
            WrapPanel.children [
                TextBlock.create [
                    TextBlock.text "Output (JA Event):"
                ]
                TextBox.create [
                    TextBox.watermark "2f 46 41 32 40"
                    TextBox.text state.jaEvent
                    TextBox.onTextChanged (fun e -> dispatch (UpdateJaEvent e))
                ]
            ]
        ]
        
    let intlSystemBox (state : State) (dispatch) =
        WrapPanel.create [
            WrapPanel.children [
                TextBlock.create [
                    TextBlock.text "Output (Intl System):"
                ]
                TextBox.create [
                    TextBox.watermark "2f 46 41 32 40"
                    TextBox.text state.intlSystem
                    TextBox.onTextChanged (fun e -> dispatch (UpdateIntlSystem e))
                ]
            ]
        ]
    
    let view (state : State) (dispatch) =
        StackPanel.create [
            StackPanel.children [
                inputBox state dispatch
                jaSystemBox state dispatch
                jaEventBox state dispatch
                intlSystemBox state dispatch
            ] 
        ]
