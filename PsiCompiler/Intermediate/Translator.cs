﻿using System;
using Psi.Runtime;
using System.Linq;

namespace Psi.Compiler
{
	using Type = Psi.Runtime.Type;
	using Boolean = Psi.Runtime.Boolean;
	public class Translator
	{
		private readonly Scope builtinScope;
		
		public Translator()
		{
			builtinScope = CreateBuiltins();
		}

		public Program Translate(params Psi.Compiler.Grammar.Module[] modules)
		{
			throw new NotImplementedException();
		}

		static Scope CreateBuiltins()
		{
			var scope = new Scope();

			InitTypeSymbol(scope.Add(new SymbolName(TypeType.Instance, "void")), Type.Void);
			InitTypeSymbol(scope.Add(new SymbolName(TypeType.Instance, "int")), Type.Integer);
			InitTypeSymbol(scope.Add(new SymbolName(TypeType.Instance, "bool")), Type.Boolean);
			InitTypeSymbol(scope.Add(new SymbolName(TypeType.Instance, "char")), Type.Character);
			InitTypeSymbol(scope.Add(new SymbolName(TypeType.Instance, "type")), Type.PsiType);
			InitTypeSymbol(scope.Add(new SymbolName(TypeType.Instance, "string")), new ArrayType(Type.Character));

			// Initialize int API
			{
				var @int = Type.Integer;
				var @bool = Type.Boolean;

				var unaOpType = new FunctionType(@int, new Parameter("val", @int));
				var binOpType = new FunctionType(@int, new Parameter("lhs", @int), new Parameter("rhs", @int));
				var relOpType = new FunctionType(@bool, new Parameter("lhs", @int), new Parameter("rhs", @int));
				var assOpType = new FunctionType(@int, new Parameter("dst", @int, ParameterFlags.Out), new Parameter("val", @int));

				// unary arithmetic
				InitFunSymbol(scope, PsiOperator.Plus, new BuiltinFunction(unaOpType, (args) =>
				{
					return args[0].Value;
				}));
				InitFunSymbol(scope, PsiOperator.Minus, new BuiltinFunction(unaOpType, (args) =>
				{
					return new Integer(-(args[0].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.Invert, new BuiltinFunction(unaOpType, (args) =>
				{
					return new Integer(~(args[0].Value as Integer));
				}));

				// binary arithmetic
				InitFunSymbol(scope, PsiOperator.Plus, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((args[0].Value as Integer) + (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.Minus, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((args[0].Value as Integer) - (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.Multiply, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((args[0].Value as Integer) * (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.Divide, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((args[0].Value as Integer) / (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.Modulo, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((args[0].Value as Integer) % (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.And, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((args[0].Value as Integer) & (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.Or, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((args[0].Value as Integer) | (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.Xor, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((args[0].Value as Integer) ^ (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.Exponentiate, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer(Pow((args[0].Value as Integer), (args[1].Value as Integer)));
				}));
				InitFunSymbol(scope, PsiOperator.ShiftLeft, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((args[0].Value as Integer) << (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.ShiftRight, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((int)((uint)(int)(args[0].Value as Integer) >> (int)(args[1].Value as Integer)));
				}));
				InitFunSymbol(scope, PsiOperator.ArithmeticShiftRight, new BuiltinFunction(binOpType, (args) =>
				{
					return new Integer((args[0].Value as Integer) >> (args[1].Value as Integer));
				}));
				
				// relational operators
				InitFunSymbol(scope, PsiOperator.More, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Integer) > (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.Less, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Integer) < (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.MoreOrEqual, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Integer) >= (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.LessOrEqual, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Integer) <= (args[1].Value as Integer));
				}));
				InitFunSymbol(scope, PsiOperator.Equals, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Integer).Value == (args[1].Value as Integer).Value);
				}));
				InitFunSymbol(scope, PsiOperator.NotEquals, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Integer).Value != (args[1].Value as Integer).Value);
				}));
				
				InitFunSymbol(scope, PsiOperator.CopyAssign, new BuiltinFunction(assOpType, (args) =>
				{
					args[0].Value = args[1].Value;
					return null;
				}));
			}

			// Initialize bool API
			{
				var @bool = Type.Boolean;

				var unaOpType = new FunctionType(@bool, new Parameter("val", @bool));
				var binOpType = new FunctionType(@bool, new Parameter("lhs", @bool), new Parameter("rhs", @bool));
				var relOpType = binOpType;
				var assOpType = new FunctionType(@bool, new Parameter("dst", @bool, ParameterFlags.Out), new Parameter("val", @bool));

				// unary arithmetic
				InitFunSymbol(scope, PsiOperator.Invert, new BuiltinFunction(unaOpType, (args) =>
				{
					return new Boolean(!(args[0].Value as Boolean));
				}));

				// binary arithmetic
				InitFunSymbol(scope, PsiOperator.And, new BuiltinFunction(binOpType, (args) =>
				{
					return new Boolean((args[0].Value as Boolean) & (args[1].Value as Boolean));
				}));
				InitFunSymbol(scope, PsiOperator.Or, new BuiltinFunction(binOpType, (args) =>
				{
					return new Boolean((args[0].Value as Boolean) | (args[1].Value as Boolean));
				}));
				InitFunSymbol(scope, PsiOperator.Xor, new BuiltinFunction(binOpType, (args) =>
				{
					return new Boolean((args[0].Value as Boolean) ^ (args[1].Value as Boolean));
				}));
				
				// relational operators
				InitFunSymbol(scope, PsiOperator.Equals, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Boolean).Value == (args[1].Value as Boolean).Value);
				}));
				InitFunSymbol(scope, PsiOperator.NotEquals, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Boolean).Value != (args[1].Value as Boolean).Value);
				}));
				
				InitFunSymbol(scope, PsiOperator.CopyAssign, new BuiltinFunction(assOpType, (args) =>
				{
					args[0].Value = args[1].Value;
					return null;
				}));
			}

			// Initialize char API
			{
				var @char = Type.Character;
				var @bool = Type.Boolean;

				var unaOpType = new FunctionType(@char, new Parameter("val", @char));
				var relOpType = new FunctionType(@bool, new Parameter("lhs", @char), new Parameter("rhs", @char));
				var assOpType = new FunctionType(@char, new Parameter("dst", @char, ParameterFlags.Out), new Parameter("val", @char));
				
				// relational operators
				InitFunSymbol(scope, PsiOperator.More, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Character).Value > (args[1].Value as Character).Value);
				}));
				InitFunSymbol(scope, PsiOperator.Less, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Character).Value < (args[1].Value as Character).Value);
				}));
				InitFunSymbol(scope, PsiOperator.MoreOrEqual, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Character).Value >= (args[1].Value as Character).Value);
				}));
				InitFunSymbol(scope, PsiOperator.LessOrEqual, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Character).Value <= (args[1].Value as Character).Value);
				}));
				InitFunSymbol(scope, PsiOperator.Equals, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Character).Value == (args[1].Value as Character).Value);
				}));
				InitFunSymbol(scope, PsiOperator.NotEquals, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as Character).Value != (args[1].Value as Character).Value);
				}));
				
				InitFunSymbol(scope, PsiOperator.CopyAssign, new BuiltinFunction(assOpType, (args) =>
				{
					args[0].Value = args[1].Value;
					return null;
				}));
			}
			

			// Initialize type API
			{
				var @type = Type.Integer;
				var @bool = Type.Boolean;

				var relOpType = new FunctionType(@bool, new Parameter("lhs", @type), new Parameter("rhs", @type));
				var assOpType = new FunctionType(@type, new Parameter("dst", @type, ParameterFlags.Out), new Parameter("val", @type));

				// relational operators
				InitFunSymbol(scope, PsiOperator.Equals, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as TypeValue).Value == (args[1].Value as TypeValue).Value);
				}));
				InitFunSymbol(scope, PsiOperator.NotEquals, new BuiltinFunction(relOpType, (args) =>
				{
					return new Boolean((args[0].Value as TypeValue).Value != (args[1].Value as TypeValue).Value);
				}));
				
				InitFunSymbol(scope, PsiOperator.CopyAssign, new BuiltinFunction(assOpType, (args) =>
				{
					args[0].Value = args[1].Value;
					return null;
				}));
			}
	
			return scope;
		}

		static Symbol InitTypeSymbol(Symbol sym, Type type)
		{
			sym.IsConst = true;
			sym.IsExported = false;
			sym.KnownValue = new TypeValue(type);
			return sym;
		}

		static Symbol InitFunSymbol(Scope scope, PsiOperator id, FunctionPrototype proto)
		{
			var sym = scope.Add(new SymbolName(proto.Type, id));
			sym.IsConst = true;
			sym.IsExported = false;
			sym.KnownValue = new Function(proto);
			return sym;
		}

		static Symbol InitFunSymbol(Scope scope, string id, FunctionPrototype proto)
		{
			var sym = scope.Add(new SymbolName(proto.Type, id));
			sym.IsConst = true;
			sym.IsExported = false;
			sym.KnownValue = new Function(proto);
			return sym;
		}

		static int Pow(int bas, int exp)
		{
			return Enumerable
				  .Repeat(bas, exp)
				  .Aggregate(1, (a, b) => a * b);
		}
	}
}
