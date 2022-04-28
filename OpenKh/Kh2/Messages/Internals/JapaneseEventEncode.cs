﻿using OpenKh.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace OpenKh.Kh2.Messages.Internals
{
    public class JapaneseEventEncode : IMessageEncode
    {
        private static readonly Dictionary<MessageCommand, KeyValuePair<byte, BaseCmdModel>> _tableCommands =
            JapaneseEventDecode._table
            .Where(x => x.Value != null && x.Value.Command != MessageCommand.PrintText)
            .GroupBy(x => x.Value.Command)
            .ToDictionary(x => x.Key, x => x.First());

        private static readonly Dictionary<char, (byte, byte)> _tableCharacters =
            GenerateCharacterDictionary();

        private static readonly Dictionary<string, byte[]> _tableComplex =
            JapaneseEventDecode._table
            .Where(x => x.Value?.Command == MessageCommand.PrintComplex)
            .Select(x => new
            {
                Key = x.Value.Text,
                Value = new byte[] { x.Key }
            })
            .Concat(new[]
            {
                new { Key = "III", Value = new byte[] { 0x1d, 0x1a} },
                new { Key = "VII", Value = new byte[] { 0x1d, 0x1b} },
                new { Key = "VIII", Value = new byte[] { 0x1d, 0x1c } },
                new { Key = "X", Value = new byte[] { 0x1d, 0x1d } },
                new { Key = "XIII", Value = new byte[] { 0x1e, 0x50 } },
                new { Key = "VI", Value = new byte[] { 0x1e, 0xB6 } },
                new { Key = "IX", Value = new byte[] { 0x1e, 0xB7 } },
            })
            .ToDictionary(x => x.Key, x => x.Value);

        private void AppendEncodedMessageCommand(List<byte> list, MessageCommandModel messageCommand)
        {
            if (messageCommand.Command == MessageCommand.PrintText)
                AppendEncodedText(list, messageCommand.Text);
            else if (messageCommand.Command == MessageCommand.PrintComplex)
                AppendEncodedComplex(list, messageCommand.Text);
            else if (messageCommand.Command == MessageCommand.Unsupported)
                list.AddRange(messageCommand.Data);
            else
                AppendEncodedCommand(list, messageCommand.Command, messageCommand.Data);
        }

        private void AppendEncodedCommand(List<byte> list, MessageCommand command, byte[] data)
        {
            if (!_tableCommands.TryGetValue(command, out var pair))
                throw new ArgumentException($"The command {command} it is not supported by the specified encoding.");

            list.Add(pair.Key);
            for (var i = 0; i < pair.Value.Length; i++)
                list.Add(data[i]);
        }

        private void AppendEncodedText(List<byte> list, string text)
        {
            foreach (var ch in text)
                AppendEncodedChar(list, ch);
        }

        private void AppendEncodedComplex(List<byte> list, string text)
        {
            if (!_tableComplex.TryGetValue(text, out var data))
                throw new ParseException(text, 0, "Complex text does not exists");

            list.AddRange(data);
        }

        private void AppendEncodedChar(List<byte> list, char ch)
        {
            if (!_tableCharacters.TryGetValue(ch, out var data))
                throw new CharacterNotSupportedException(ch);

            if (data.Item1 != 0)
                list.Add(data.Item1);
            list.Add(data.Item2);
        }

        public byte[] Encode(List<MessageCommandModel> messageCommands)
        {
            var list = new List<byte>(100);
            foreach (var model in messageCommands)
                AppendEncodedMessageCommand(list, model);

            return list.ToArray();
        }

        private static Dictionary<char, (byte, byte)> GenerateCharacterDictionary()
        {
            var pairs = GenerateCharacterKeyValuePair(MessageCommand.PrintText)
                .Concat(GenerateCharacterKeyValuePairFromTable(MessageCommand.Table2, JapaneseEventTable._table2))
                .Concat(GenerateCharacterKeyValuePairFromTable(MessageCommand.Table3, JapaneseEventTable._table3))
                .Concat(GenerateCharacterKeyValuePairFromTable(MessageCommand.Table4, JapaneseEventTable._table4))
                .Concat(GenerateCharacterKeyValuePairFromTable(MessageCommand.Table5, JapaneseEventTable._table5))
                .Concat(GenerateCharacterKeyValuePairFromTable(MessageCommand.Table6, JapaneseEventTable._table6))
                .Concat(GenerateCharacterKeyValuePairFromTable(MessageCommand.Table7, JapaneseEventTable._table7))
                .Concat(GenerateCharacterKeyValuePairFromTable(MessageCommand.Table8, JapaneseEventTable._table8));

            return pairs
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.First().Value);
        }

        private static IEnumerable<KeyValuePair<char, (byte, byte)>> GenerateCharacterKeyValuePair(
            MessageCommand messageCommand) =>
            JapaneseEventDecode._table
                   .Where(x => x.Value?.Command == messageCommand)
                   .Select(x => new KeyValuePair<char, (byte, byte)>(x.Value.Text[0], (0, x.Key)));

        private static IEnumerable<KeyValuePair<char, (byte, byte)>> GenerateCharacterKeyValuePairFromTable(
            MessageCommand messageCommand, char[] table) =>
            JapaneseEventDecode._table
                .Where(x => x.Value?.Command == messageCommand)
                .Select(x => table.Select((ch, i) => new
                {
                    TableId = x.Key,
                    Character = ch,
                    Data = (byte)i
                }))
                .SelectMany(x => x)
                .Where(x => x.Character != '_' && x.Character != '?')
                .Select(x => new KeyValuePair<char, (byte, byte)>(x.Character, (x.TableId, x.Data)));
    }
}
