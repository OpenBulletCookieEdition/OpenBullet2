﻿using RuriLib.Exceptions;
using RuriLib.Helpers.Blocks;
using RuriLib.Models.Blocks;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RuriLib.Helpers.Transpilers
{
    public class Loli2StackTranspiler
    {
        private readonly string validTokenRegex = "[A-Za-z][A-Za-z0-9_]*";

        public List<BlockInstance> Transpile(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
                return new List<BlockInstance>();

            var lines = script.Split(new string[] { "\n", "\r\n" }, System.StringSplitOptions.None);
            List<BlockInstance> stack = new List<BlockInstance>();

            var blockFactory = new BlockFactory();
            int lineNumber = 0;
            string line, trimmedLine;

            while (lineNumber < lines.Length)
            {
                line = lines[lineNumber];
                trimmedLine = line.Trim();
                lineNumber++;

                // If it's a block directive
                if (trimmedLine.StartsWith("BLOCK:"))
                {
                    /* 
                        BLOCK:Id
                        ...
                        ENDBLOCK
                    */

                    var match = Regex.Match(trimmedLine, $"^BLOCK:({validTokenRegex})$");

                    if (!match.Success)
                        throw new LoliCodeParsingException(lineNumber, "Could not parse the block id");

                    string blockId = match.Groups[1].Value;

                    // Create the block
                    var block = blockFactory.GetBlock<BlockInstance>(blockId);

                    StringBuilder sb = new StringBuilder();

                    // As long as we don't find the ENDBLOCK token, add lines to the StringBuilder
                    while (lineNumber < lines.Length)
                    {
                        line = lines[lineNumber];
                        trimmedLine = line.Trim();
                        lineNumber++;

                        if (line.StartsWith("ENDBLOCK"))
                            break;

                        sb.AppendLine(trimmedLine);
                    }

                    string blockOptions = sb.ToString();

                    block.FromLC(ref blockOptions);
                    stack.Add(block);
                }

                // If it's not a block directive, build a LoliCode block
                else
                {
                    var descriptor = new LoliCodeBlockDescriptor();
                    var block = new LoliCodeBlockInstance(descriptor);

                    using var writer = new StringWriter();
                    writer.WriteLine(line);

                    // As long as we don't find a BLOCK directive, add lines to the StringBuilder
                    while (lineNumber < lines.Length)
                    {
                        line = lines[lineNumber];
                        trimmedLine = line.Trim();
                        lineNumber++;

                        // If we find a block directive, stop reading lines without consuming it
                        if (trimmedLine.StartsWith("BLOCK:"))
                        {
                            lineNumber--;
                            break;
                        }

                        writer.WriteLine(line);
                    }

                    block.Script = writer.ToString();

                    // Make sure the script is not empty
                    if (!string.IsNullOrWhiteSpace(block.Script.Replace("\n", "").Replace("\r\n", "")))
                        stack.Add(block);
                }
            }

            return stack;
        }
    }
}