﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Psi.Compiler.Grammar;

namespace Psi.Compiler.Test
{
	[TestFixture]
	public class Test_AST_TypeLiterals : Test_AST_Base
	{
		[Test]
		public void EmptyEnumTypeLiteral()
		{
			var module = Load("const name = enum();");
			Assert.IsNull(module);
		}
		
		[Test]
		public void SingleEnumTypeLiteral()
		{
			var module = Load("type name = enum(a);");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(EnumTypeLiteral), expression);
			var val = (EnumTypeLiteral)expression;

			Assert.NotNull(val.Items);
			Assert.AreEqual(1, val.Items.Count);
			Assert.AreEqual("a", val.Items[0]);
		}
		
		[Test]
		public void MultipleEnumTypeLiteral()
		{
			var module = Load("type name = enum(a,b,c);");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(EnumTypeLiteral), expression);
			var val = (EnumTypeLiteral)expression;

			Assert.NotNull(val.Items);
			Assert.AreEqual(3, val.Items.Count);
			Assert.AreEqual("a", val.Items[0]);
			Assert.AreEqual("b", val.Items[1]);
			Assert.AreEqual("c", val.Items[2]);
		}
		
		[Test]
		public void EmptyRecordTypeLiteral()
		{
			var module = Load("type name = record();");
			Assert.IsNull(module);
		}
		
		[Test]
		public void SingleRecordTypeLiteral()
		{
			var module = Load("type name = record(a : int);");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(RecordTypeLiteral), expression);
			var val = (RecordTypeLiteral)expression;

			Assert.NotNull(val.Fields);
			Assert.AreEqual(1, val.Fields.Count);

			var field = val.Fields[0];
			
			Assert.NotNull(field);

			Assert.IsFalse(field.IsConst);
			Assert.IsFalse(field.IsExported);
			Assert.IsTrue(field.IsField);

			Assert.AreEqual(field.Name, "a");
			
			Assert.IsInstanceOf(typeof(NamedTypeLiteral), field.Type);
			Assert.AreEqual("int", ((NamedTypeLiteral)field.Type).Name.ToString());
			
			Assert.IsNull(field.Value);
		}
		
		[Test]
		public void MultipleRecordTypeLiteral()
		{
			var module = Load("type name = record(a : int, b : string);");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(RecordTypeLiteral), expression);
			var val = (RecordTypeLiteral)expression;

			Assert.NotNull(val.Fields);
			Assert.AreEqual(2, val.Fields.Count);

			Assert.NotNull(val.Fields[0]);
			Assert.NotNull(val.Fields[1]);
		}

		[Test]
		public void DefaultValueRecordTypeLiteral()
		{
			var module = Load("type name = record(a = 10);");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(RecordTypeLiteral), expression);
			var val = (RecordTypeLiteral)expression;

			Assert.NotNull(val.Fields);
			Assert.AreEqual(1, val.Fields.Count);

			var field = val.Fields[0];
			
			Assert.NotNull(field);

			Assert.AreEqual(field.Name, "a");
			Assert.AreSame(field.Type, PsiParser.Undefined);
			Assert.IsInstanceOf(typeof(NumberLiteral), field.Value);
		}

		[Test]
		public void TypedDefaultValueRecordTypeLiteral()
		{
			var module = Load("type name = record(a : int = 10);");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(RecordTypeLiteral), expression);
			var val = (RecordTypeLiteral)expression;

			Assert.NotNull(val.Fields);
			Assert.AreEqual(1, val.Fields.Count);

			var field = val.Fields[0];
			
			Assert.NotNull(field);

			Assert.AreEqual(field.Name, "a");
			
			Assert.IsInstanceOf(typeof(NamedTypeLiteral), field.Type);
			Assert.AreEqual("int", ((NamedTypeLiteral)field.Type).Name.ToString());
			
			Assert.IsInstanceOf(typeof(NumberLiteral), field.Value);
		}
		
		
		
		[Test]
		public void EmptyTypedEnumTypeLiteral()
		{
			var module = Load("type name = enum<float>();");
			Assert.IsNull(module);
		}
		
		[Test]
		public void SingleTypedEnumTypeLiteral()
		{
			var module = Load("type name = enum<float>(a = 10);");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(TypedEnumTypeLiteral), expression);
			var val = (TypedEnumTypeLiteral)expression;

			Assert.NotNull(val.Items);
			Assert.AreEqual(1, val.Items.Count);

			var item = val.Items[0];
			
			Assert.NotNull(item);

			Assert.IsFalse(item.IsConst);
			Assert.IsFalse(item.IsExported);
			Assert.IsTrue(item.IsField);

			Assert.AreEqual(item.Name, "a");

			Assert.AreSame(PsiParser.Undefined, item.Type);

			Assert.IsInstanceOf(typeof(NumberLiteral), item.Value);
			Assert.AreEqual("10", ((NumberLiteral)item.Value).Value);
		}
		
		[Test]
		public void MultipleTypedEnumTypeLiteral()
		{
			var module = Load("type name = enum<float>(a = 10, b = 20);");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(TypedEnumTypeLiteral), expression);
			var val = (TypedEnumTypeLiteral)expression;

			Assert.NotNull(val.Items);
			Assert.AreEqual(2, val.Items.Count);

			Assert.AreEqual("a", val.Items[0].Name);
			Assert.AreSame(PsiParser.Undefined, val.Items[0].Type);
			Assert.IsInstanceOf(typeof(NumberLiteral), val.Items[0].Value);
			Assert.AreEqual("10", ((NumberLiteral)val.Items[0].Value).Value);
			
			Assert.AreEqual("b", val.Items[1].Name);
			Assert.AreSame(PsiParser.Undefined, val.Items[1].Type);
			Assert.IsInstanceOf(typeof(NumberLiteral), val.Items[1].Value);
			Assert.AreEqual("20", ((NumberLiteral)val.Items[1].Value).Value);
		}
		
		[Test]
		public void ReferenceType()
		{
			var module = Load("type name = ref<int>;");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(ReferenceTypeLiteral), expression);
			var val = (ReferenceTypeLiteral)expression;

			Assert.NotNull(val.ObjectType);

			Assert.IsInstanceOf(typeof(NamedTypeLiteral), val.ObjectType);
			Assert.AreEqual("int", ((NamedTypeLiteral)val.ObjectType).Name.ToString());
		}
		
		[Test]
		public void SimpleArrayType()
		{
			var module = Load("type name = array<int>;");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(ArrayTypeLiteral), expression);
			var val = (ArrayTypeLiteral)expression;

			Assert.NotNull(val.ElementType);
			Assert.AreEqual(1, val.Dimensions);

			Assert.IsInstanceOf(typeof(NamedTypeLiteral), val.ElementType);
			Assert.AreEqual("int", ((NamedTypeLiteral)val.ElementType).Name.ToString());
		}
		
		[Test]
		public void MultidimArrayType()
		{
			var module = Load("type matrix = array<float,42>;");
			var expression = module.TypeDeclarations[0].Type;

			Assert.IsInstanceOf(typeof(ArrayTypeLiteral), expression);
			var val = (ArrayTypeLiteral)expression;

			Assert.NotNull(val.ElementType);
			Assert.AreEqual(42, val.Dimensions);

			Assert.IsInstanceOf(typeof(NamedTypeLiteral), val.ElementType);
			Assert.AreEqual("float", ((NamedTypeLiteral)val.ElementType).Name.ToString());
		}
		
		// TODO: Test for function type literals
	}
}
