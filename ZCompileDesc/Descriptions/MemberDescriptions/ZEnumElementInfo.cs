﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ZCompileDesc.Words;
using ZLangRT.Utils;

namespace ZCompileDesc.Descriptions
{
    public class ZEnumElementInfo : ZMemberInfo
    {
        public ZEnumType ZEnum { get; private set; }
        public FieldInfo MarkField { get; private set; }
        public FieldInfo SharpField { get; private set; }

        public ZEnumElementInfo(FieldInfo fieldInfo, ZEnumType zenum)
        {
            MarkField = fieldInfo;
            SharpField = fieldInfo;
            ZEnum = zenum;
            Init();
        }

        public ZEnumElementInfo(FieldInfo zfieldInfo, FieldInfo sfieldInfo, ZEnumType zenum)
        {
            MarkField = zfieldInfo;
            SharpField = sfieldInfo;
            ZEnum = zenum;
            Init();
        }

        private void Init()
        {
            IsStatic = SharpField.IsStatic;
            ZNames = ZDescriptionHelper.GetZNames(MarkField);
            CanRead = true;
            CanWrite = false;
            AccessAttribute = AccessAttributeEnum.Public;
        }

        public object Value
        {
            get
            {
                return SharpField.GetValue(null);
            }
        }

        //public override WordInfo[] GetWordInfos()
        //{
        //    List<WordInfo> words = new List<WordInfo>();
        //    foreach (var zname in ZNames)
        //    {
        //        WordInfo info = new WordInfo(zname, WordKind.EnumElement,this);
        //        words.Add(info);
        //    }
        //    return words.ToArray();
        //}

        public override WordInfo SearchWord(string text)
        {
            if (!HasZName(text)) return null;
            WordInfo info = new WordInfo(text, WordKind.EnumElement, this);
            return info;
        }

        public override string SharpMemberName
        {
            get { return this.MarkField.Name; }
        }

        public override ZType MemberZType
        {
            get { return ZTypeManager.GetBySharpType(SharpField.FieldType) as ZType; }
        }

        public override string ToString()
        {
            return this.MarkField.Name + "(" + string.Join(",", ZNames) + ")";
        }
    }
}
