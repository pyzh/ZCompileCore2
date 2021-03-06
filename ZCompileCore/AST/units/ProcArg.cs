﻿using ZCompileCore.Contexts;
using ZCompileCore.Lex;
using ZCompileCore.Parsers;
using ZCompileCore.Symbols;
using ZCompileDesc.Descriptions;
using ZCompileDesc.Words;

namespace ZCompileCore.AST
{
    public class ProcArg : UnitBase
    {
        public Token ArgToken;
        string ArgText;

        public string ArgName;
        public ZType ArgZType;
        public ContextProc ProcContext;

        public void Analy(NameTypeParser parser )
        {
            ArgText = ArgToken.GetText();
            if (AnalyNameType(parser))
            {
                SymbolArg argSymbol = new SymbolArg(ArgName, ArgZType, this.ProcContext.CreateArgIndex(ArgName));
                this.ProcContext.Symbols.Add(argSymbol);
                WordInfo word = new WordInfo(ArgName, WordKind.ArgName,this);
                this.ProcContext.ProcVarWordDictionary.Add(word);
            }
        }

        public bool AnalyNameType(NameTypeParser parser)
        {
            NameTypeParser.ParseResult result = parser.ParseVar(ArgToken);
            if (result != null)
            {
                ArgName = result.VarName;
                ArgZType = result.ZType;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return string.Format("({0})", ArgToken.GetText());
        }
    }
}
