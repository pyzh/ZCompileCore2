﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZCompileCore.AST;
using ZCompileCore.Contexts;
using ZCompileCore.Lex;

namespace ZCompileCore.Parsers
{
    public class FileSectionParser
    {
        TokenTape tape;

        public FileSectionParser()
        {
           
        }

        FileMutilType fileMY;

        public FileMutilType Parse(List<Token> tokens, ContextFile fileContext)
        {
            tape = new TokenTape(tokens, fileContext);
            fileMY = new FileMutilType();
            fileMY.FileContext = fileContext;

            while (tape.CurrentKind != TokenKind.EOF)
            {
                TokenKind nkind = tape.Next.Kind;
                if (nkind == TokenKind.Colon && tape.CurrentKind== TokenKind.Ident)
                {
                    SectionBase section = ParseSection();
                    if (section != null)
                    {
                        fileMY.AddSection(section);
                    }
                }
                else if(tape.CurrentKind== TokenKind.NewLine)
                {
                    SkipNewLine();
                }
                else
                {
                    SectionProc section = ParseProc();
                    if (section != null)
                    {
                        if (section.NamePart.IsConstructor())
                        {
                            SectionConstructor constructor = new SectionConstructor(section);
                            fileMY.AddSection(constructor);
                        }
                        else
                        {
                            fileMY.AddSection(section);
                        }
                    }
                }
            }
            return fileMY;
        }

        private SectionBase ParseSection()
        {
            string headText = tape.Current.GetText();
            if (headText.StartsWith("导入"))
            {
                SectionImport section = ParseImport();
                return section;
            }
            else if (headText.StartsWith("使用"))
            {
                SectionUse section = ParseUse();
                return section;
            }
            else if (headText.StartsWith("约定"))
            {
                SectionEnum section = ParseEnum();
                return section;
            }
            else if (headText.StartsWith("声明"))
            {
                SectionDim section = ParseDim();
                return section;
            }
            else if (headText.EndsWith("类型"))
            {
                SectionClassName section = ParseClass();
                return section;
            }
            else if (headText.EndsWith("属性"))
            {
                SectionProperties section = ParseProperties();
                return section;
            }
            else
            {
                SectionProc section = ParseProc();
                if(section.NamePart.IsConstructor())
                {
                    SectionConstructor constructor = new SectionConstructor(section);
                    return constructor;
                }
                else
                {
                    return section;
                }        
                //tape.error("错误的段落");
                //tape.MoveNext();
                //return null;
            }
        }

        #region parse import

        private SectionImport ParseImport()
        {
            SectionImport ast = new SectionImport();
            ast.KeyToken = tape.Current;
            tape.MoveNext();
            tape.Match(TokenKind.Colon);
            ast.Packages = ParsePackageList();
            return ast;
        }

        private bool IsImportPackagePart(TokenKind kind)
        {
            return (kind == TokenKind.Ident ||  kind == TokenKind.DIV || kind == TokenKind.Comma);
        }

        private List<PackageNameAST> ParsePackageList()
        {
            List<PackageNameAST> list = new List<PackageNameAST>();
            while (IsImportPackagePart(tape.Current.Kind))
            {
                if (tape.Current.Kind == TokenKind.Ident)
                {
                    PackageNameAST pname = ParsePackageName(tape);
                    if (pname != null)
                    {
                        list.Add(pname);
                    }
                }
                else if (tape.CurrentKind == TokenKind.Comma)
                {
                    tape.MoveNext();
                }
                //else if (tape.CurrentKind == TokenKind.NewLine)
                //{
                //    SkipNewLine();
                //}
                //else
                //{
                //    tape.error(string.Format("'{0}'不是正确的导入名称", tape.Current.GetText()));
                //    tape.MoveNext();
                //}
            }
            if(tape.CurrentKind== TokenKind.Semi)
            {
                tape.MoveNext();
            }
            SkipNewLine();
            //tape.Match(TokenKind.Semi);
            return list;
        }

        private bool IsPackageNamePart(TokenKind kind)
        {
            return (kind == TokenKind.Ident || kind == TokenKind.DIV);
        }

        private PackageNameAST ParsePackageName(TokenTape tape)
        {
            PackageNameAST pname = new PackageNameAST();
            //TokenKind currentKind = tape.Current.Kind;
            while (IsPackageNamePart(tape.CurrentKind))
            {
                //currentKind = tape.Current.Kind;
                if (tape.CurrentKind == TokenKind.Ident)
                {
                    pname.Add(tape.Current);
                    tape.MoveNext();
                }
                else if (tape.CurrentKind == TokenKind.DIV)
                {
                    tape.MoveNext();
                }
                //else if (tape.CurrentKind == TokenKind.Semi)
                //{
                //    break;
                //}
                //else if (tape.CurrentKind == TokenKind.NewLine)
                //{
                //    break;
                //}
                //else
                //{
                //    tape.error(string.Format("'{0}'不是正确的开发包名称", tape.Current.GetText()));
                //    tape.MoveNext();
                //}
            }
            return pname;
        }
        #endregion

        #region parse use

        private SectionUse ParseUse()
        {
            SectionUse ast = new SectionUse();

            ast.KeyToken = tape.Current;
            tape.MoveNext();
            tape.Match(TokenKind.Colon);

            while (tape.CurrentKind != TokenKind.NewLine && tape.CurrentKind != TokenKind.EOF)
            {
                if (tape.CurrentKind == TokenKind.Ident)
                {
                    ast.AddTypeToken(tape.Current);
                    tape.MoveNext();
                }
                else if (tape.CurrentKind == TokenKind.Comma)
                {
                    tape.MoveNext();
                }
                else
                {
                    tape.error(string.Format("使用的'{0}'不是正确的类型名称", tape.Current.GetText()));
                    tape.MoveNext();
                }
            }
            SkipNewLine();
            return ast;
        }
        #endregion

        #region parse enum
        private SectionEnum ParseEnum()
        {
            SectionEnum ast = new SectionEnum();
            Token headToken = tape.Current;
            ast.KeyToken = new Token("声明", TokenKind.Ident, headToken.Line,headToken.Col);
            ast.NameToken = new Token(headToken.GetText().Substring(2), TokenKind.Ident, headToken.Line, headToken.Col+2);
            tape.MoveNext();
            tape.Match(TokenKind.Colon);
            //TokenTape tape = new TokenTape(sectionRaw.Content);
            while (tape.CurrentKind != TokenKind.EOF)
            {
                if (tape.CurrentKind == TokenKind.Ident)
                {
                    ast.AddValueToken(tape.Current);
                    tape.MoveNext();
                }
                else if (tape.CurrentKind == TokenKind.Comma)
                {
                    tape.MoveNext();
                }
                else
                {
                    tape.error(string.Format("定义的约定'{0}'不是正确的名称", tape.Current.GetText()));
                    tape.MoveNext();
                }
            }
            return ast;
        }
        #endregion

        #region parse dim

        private SectionDim ParseDim()
        {
            SectionDim ast = new SectionDim();
            Token headToken = tape.Current;
            if (headToken.GetText() == "声明")
            {
                ast.KeyToken = headToken;
            }
            else
            {
                ast.KeyToken = new Token("声明", TokenKind.Ident, headToken.Line, headToken.Col);
                ast.NameToken = new Token(headToken.GetText().Substring(2), TokenKind.Ident, headToken.Line, headToken.Col + 2);
            }
            tape.MoveNext();
            tape.Match(TokenKind.Colon);
            ast.Dims = ParseDimList();
            return ast;
        }

        private List<SectionDim.DimVarAST> ParseDimList()
        {
            List<SectionDim.DimVarAST> list = new List<SectionDim.DimVarAST>();
            while (tape.CurrentKind != TokenKind.NewLine && tape.CurrentKind != TokenKind.EOF)
            {
                if (tape.Current.Kind == TokenKind.Ident)
                {
                    SectionDim.DimVarAST dimv = ParseDimVar(tape);
                    if (dimv != null)
                    {
                        list.Add(dimv);
                    }
                }
                if (tape.CurrentKind == TokenKind.NewLine)
                {
                    break;
                }
                else if (tape.CurrentKind == TokenKind.Comma)
                {
                    tape.MoveNext();
                }
                else
                {
                    tape.error(string.Format("'{0}'不是正确的声明", tape.Current.GetText()));
                    tape.MoveNext();
                }
            }
            return list;
        }

        private SectionDim.DimVarAST ParseDimVar(TokenTape tape)
        {
            SectionDim.DimVarAST dimv = new SectionDim.DimVarAST();
            dimv.NameToken = tape.Current;
            tape.MoveNext();
            tape.Match(TokenKind.Assign);
            dimv.TypeToken = tape.Current;
            tape.MoveNext();
            return dimv;
        }
        #endregion

        #region parse class

        private SectionClassName ParseClass()
        {
            SectionClassName ast = new SectionClassName();
            Token headToken = tape.Current;
            string headFirstText = headToken.GetText();
            if (headFirstText.Length > 2)
            {
                string baseTypeName = headFirstText.Substring(0, headFirstText.Length - 2);
                ast.BaseTypeToken = new Token(baseTypeName, TokenKind.Ident, headToken.Line, headToken.Col);
            }
            else
            {
                ast.BaseTypeToken = null;
            }
            tape.MoveNext();
            tape.Match(TokenKind.Colon);
            while (tape.CurrentKind != TokenKind.EOF && tape.CurrentKind != TokenKind.NewLine)
            {
                if (tape.CurrentKind == TokenKind.Ident)
                {
                    ast.NameToken = tape.Current;
                    tape.MoveNext();
                }
                else if (tape.CurrentKind == TokenKind.Comma)
                {
                    tape.MoveNext();
                }
                else
                {
                    tape.error(string.Format("类型'{0}'不是正确的名称", tape.Current.GetText()));
                    tape.MoveNext();
                }
            }
            if (tape.CurrentKind == TokenKind.NewLine) tape.MoveNext();
            return ast;
        }

        #endregion

        #region parse Properties

        private SectionProperties ParseProperties()
        {
            SectionProperties ast = new SectionProperties();
            Token headFistToken = tape.Current;
            string headFirstText = headFistToken.GetText();
            if (headFirstText.Length > 2)
            {
                string baseTypeName = headFirstText.Substring(0, headFirstText.Length - 2);
                ast.TypeToken = new Token(baseTypeName, TokenKind.Ident, headFistToken.Line, headFistToken.Col);
                ast.KeyToken = new Token("属性", TokenKind.Ident, headFistToken.Line, headFistToken.Col + headFirstText.Length - 2);
            }
            else
            {
                ast.KeyToken = headFistToken;
            }

            tape.MoveNext();
            tape.Match(TokenKind.Colon);

            while (tape.CurrentKind != TokenKind.EOF && tape.CurrentKind!= TokenKind.NewLine)
            {
                if (tape.CurrentKind == TokenKind.Ident)
                {
                    PropertyAST property = ParseProperty();
                    if (property != null)
                    {
                        ast.AddProperty(property);
                    }
                }
                else if (tape.CurrentKind == TokenKind.Comma)
                {
                    tape.MoveNext();
                }
                else
                {
                    tape.error(string.Format("类型'{0}'不是正确的属性", tape.Current.GetText()));
                    tape.MoveNext();
                }
            }
            SkipNewLine();
            return ast;
        }

        private PropertyAST ParseProperty( )
        {
            PropertyAST property = new PropertyAST();
            //property.SetFileContext(this.filem.FileContext);//.FileContext = this.filem.FileContext;
            property.FileContext = this.fileMY.FileContext;

            property.NameToken = tape.Current;
            tape.MoveNext();
            if (tape.CurrentKind == TokenKind.Assign)
            {
                tape.MoveNext();
                Exp exp = ParseRawExpPropertyValue();
                if (exp != null)
                {
                    property.PropertyValue = exp;
                }
            }
            return property;
        }
        #endregion

        #region parse raw exp

        private Exp ParseRawExpPropertyValue()
        {
            ExpRaw rexp = new ExpRaw();
            int line = tape.Current.Line;
            while (tape.CurrentKind != TokenKind.EOF && tape.CurrentKind != TokenKind.NewLine && tape.CurrentKind != TokenKind.Comma)
            {
                rexp.RawTokens.Add(tape.Current);
                tape.MoveNext();
                if (tape.CurrentKind == TokenKind.Semi)
                {
                    break;
                }
            }
            return rexp;
        }

        private Exp ParseRawExpLine()
        {
            ExpRaw rexp = new ExpRaw();
            int line = tape.Current.Line;
            int bracketCount = -1;
            while (tape.Current.Kind != TokenKind.EOF && tape.Current.Kind != TokenKind.NewLine)
            {
                TokenKind kind = tape.Current.Kind;
                if(kind== TokenKind.LBS)
                {
                    rexp.RawTokens.Add(tape.Current);
                    if (bracketCount==-1)
                    {
                        bracketCount = 0;
                    }
                    bracketCount++;
                    tape.MoveNext();
                }
                else if (kind == TokenKind.RBS)
                {
                    if (bracketCount <= 0)
                    {
                        tape.error("多余的右括号");
                    }
                    else
                    {
                        bracketCount--;
                        rexp.RawTokens.Add(tape.Current);
                    }
                    tape.MoveNext();
                }
                //if (kind == TokenKind.NewLine)
                //{
                //    break;
                //}
                else if(kind== TokenKind.Comma)
                {
                    if(bracketCount==-1)
                    {
                        rexp.RawTokens.Add(tape.Current);
                        tape.MoveNext();
                        break;
                    }
                    else if (bracketCount == 0)
                    {
                        tape.MoveNext();
                        break;
                    }
                    else if (bracketCount >0)
                    {
                        rexp.RawTokens.Add(tape.Current);
                        tape.MoveNext();
                    }
                }
                else
                {
                    rexp.RawTokens.Add(tape.Current);
                    tape.MoveNext();
                }
                //if (tape.CurrentKind == TokenKind.Semi)
                //{
                //    break;
                //}
            }
            return rexp;
        }
        #endregion

        #region Parse 过程
        private SectionProc ParseProc()
        {
            SectionProc ast = new SectionProc();
            //TokenTape nameTape = new TokenTape(sectionRaw.Content);
            ast.NamePart = ParseProcName();
            tape.Match(TokenKind.Colon);
            ast.RetToken = ParseRetProc();
            if (ast.NamePart == null)
            {
                return null;
            }
            ast.Body = ParseProcBody();
            return ast;
        }

        private Token ParseRetProc( )
        {
            if (tape.CurrentKind == TokenKind.Ident)
            {
                Token headToken = tape.Current;
                tape.MoveNext();
                if(tape.CurrentKind== TokenKind.NewLine)
                {
                    SkipNewLine();
                }
                return headToken;
            }
            return null;
        }

        private ProcName ParseProcName( )
        {
            ProcName procName = new ProcName();
            procName.FileContext = this.fileMY.FileContext;
            while (tape.CurrentKind != TokenKind.EOF && tape.CurrentKind!= TokenKind.NewLine)
            {
                if (tape.CurrentKind == TokenKind.LBS)
                {
                    var arg = ParseProcArg();
                    if (arg != null)
                    {
                        procName.AddArg(arg);
                    }
                }
                else if (tape.CurrentKind == TokenKind.Ident)
                {
                    procName.AddNamePart(tape.Current);
                    tape.MoveNext();
                }
                else if (tape.CurrentKind == TokenKind.Colon)
                {
                    break;
                }
            }
            return procName;
        }

        private ProcArg ParseProcArg( )
        {
            ProcArg arg = new ProcArg();
            arg.FileContext = this.fileMY.FileContext;
            tape.Match(TokenKind.LBS);
            if (tape.CurrentKind == TokenKind.Ident)
            {
                arg.ArgToken = tape.Current;
                tape.MoveNext();
            }
            tape.Match(TokenKind.RBS);
            if (arg.ArgToken == null) return null;
            return arg;
        }

        private StmtBlock ParseProcBody()
        {
            CodePosition startPosition = new CodePosition(tape.Current.Line,1);
            return ParseStmtBlock(startPosition);
        }
        #endregion

        #region Parse stmt 语句

        private Stmt ParseStmt()
        {
            if(tape.CurrentKind== TokenKind.NewLine)
            {
                SkipNewLine();
            }
            //Stmt stmt = null;
            TokenKind kind = tape.Current.Kind;//.CurrentKind;
            if (kind == TokenKind.EOF)
            {
                //bodyIndex++;
                return null;
            }
            else if (kind == TokenKind.IF)
            {
                Stmt stmt = ParseIf();
                return stmt;
            }
            else if (kind == TokenKind.While)
            {
                Stmt stmt = ParseWhile();
                return stmt;
            }
            else if (kind == TokenKind.Catch)
            {
                Stmt stmt = ParseCatch();
                return stmt;
            }
            else if (kind == TokenKind.Comma)
            {
                tape.MoveNext();
                Stmt stmt = ParseStmt();
                return stmt;
            }
            else
            {
                Stmt stmt = ParseStmtCall();
                return stmt;
            }
            //if (stmt != null)
            //{
            //    stmt.Deep = deep;
            //}
            //return stmt;
        }

        private Stmt ParseStmtCall()
        {
            Exp startExpr = ParseRawExpLine();
            StmtCall stmtcall = new StmtCall();
            stmtcall.CallExp = startExpr;
            if (tape.CurrentKind == TokenKind.Comma )
            {
                tape.Match(TokenKind.Comma);
                return stmtcall;
            }
            else if ( tape.CurrentKind == TokenKind.NewLine)
            {
                SkipNewLine();
                return stmtcall;
            }
            else if (tape.CurrentKind == TokenKind.EOF )
            {
                return stmtcall;
            }
            else
            {
                return stmtcall;
                //tape.error("ParseAssignOrExprStmt 无法识别" + tape.Current.ToCode());
                //tape.MoveNext();
                //return null;
            }
        }

        private Stmt ParseCatch()
        {
            StmtCatch catchStmt = new StmtCatch();
            catchStmt.CatchToken = tape.Current;
            tape.MoveNext();
            catchStmt.ExceptionTypeVarToken = tape.Current;
            tape.MoveNext();
            catchStmt.CatchBody = ParseStmtBlock(catchStmt.CatchToken.Position);
            return catchStmt;
        }

        private Stmt ParseWhile()
        {
            StmtWhile whileStmt = new StmtWhile();
            whileStmt.WhileToken = tape.Current;
            tape.MoveNext();
            whileStmt.Condition = ParseRawExpLine();
            whileStmt.WhileBody = ParseStmtBlock(whileStmt.WhileToken.Position);
            return whileStmt;
        }

        private Stmt ParseIf()
        {
            StmtIf ifStmt = new StmtIf();
            ifStmt.IfToken = tape.Current;

            StmtIf.StmtIfTrue ifPart = ParseTruePart();
            ifStmt.Parts.Add(ifPart);
            while (tape.CurrentKind == TokenKind.ELSEIF)
            {
                StmtIf.StmtIfTrue elseifPart = ParseTruePart();
                ifStmt.Parts.Add(elseifPart);
            }
            if (tape.CurrentKind == TokenKind.ELSE)
            {
                CodePosition pos = tape.Current.Position;
                tape.MoveNext();
                ifStmt.ElsePart = ParseStmtBlock(pos);
            }
            return ifStmt;
        }

        private StmtIf.StmtIfTrue ParseTruePart()
        {
            StmtIf.StmtIfTrue eistmt = new StmtIf.StmtIfTrue();
            eistmt.KeyToken = tape.Current;
            tape.MoveNext();//跳过否则如果
            eistmt.Condition = ParseRawExpLine();
            eistmt.Body = ParseStmtBlock(eistmt.KeyToken.Position);
            return eistmt;
        }

        private StmtBlock ParseStmtBlock(CodePosition startPosition)
        {
            StmtBlock stmtBlock = new StmtBlock();
            while (tape.Current.Col > startPosition.Col)
            {
                Stmt stmt = ParseStmt();
                if (stmt != null)
                {
                    stmtBlock.StmtList.Add(stmt);
                }
            }
            return stmtBlock;
        }

        #endregion

        private void SkipNewLine()
        {
            while(tape.CurrentKind== TokenKind.NewLine)
            {
                tape.MoveNext();
            }
        }
    }
}
