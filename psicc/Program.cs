﻿using System;
using System.IO;

namespace Psi.Compiler
{
    using LLVMSharp;
    using Psi.Compiler.Codegen;
    using Psi.Compiler.Grammar;
    using Psi.Compiler.Intermediate;
    using System.Diagnostics;

    class MainClass
    {
        public static void Main(string[] args)
        {
            WrappedMain(args);
            if (Debugger.IsAttached && Environment.OSVersion.Platform != PlatformID.Unix)
                Console.ReadLine();
        }

        public static void WrappedMain(string[] args)
        {
            CodeGenerator.module = LLVM.ModuleCreateWithName("psi.result");

            // primitives
            TypeMapper.Add(typeof(bool), BuiltinType.Boolean);
            TypeMapper.Add(typeof(byte), BuiltinType.Byte);
            TypeMapper.Add(typeof(long), BuiltinType.Integer);
            TypeMapper.Add(typeof(ulong), BuiltinType.UnsignedInteger);
            TypeMapper.Add(typeof(double), BuiltinType.Real);
            TypeMapper.Add(typeof(string), BuiltinType.String);
            TypeMapper.Add(typeof(int), BuiltinType.Character);

            // required types
            TypeMapper.Add(typeof(void), Type.VoidType);
            TypeMapper.Add(typeof(Type), Type.MetaType);
            TypeMapper.Add(typeof(Intermediate.Module), Type.ModuleType);

            var std = CreateStd();

            var syntaxModule = Load("../Sources/CompilerTest.psi");

            if (syntaxModule == null)
            {
                Console.WriteLine("Failed to parse!");
                return;
            }

            var printer = new ModulePrinter(Console.Out);
            printer.Print(syntaxModule);

            var declarableGlobalScope = new SimpleScope
                {
                    new Symbol(Type.ModuleType, "std")
                    {
                        Initializer = new ModuleLiteral(std),
                        IsConst = true,
                        IsExported = false
                    }
                };
            InitializeGlobalOperators(declarableGlobalScope);

            // TODO: Add "linked libraries" to declarableGlobalScope

            var globalScope = new StackableScope();
            globalScope.Push(new AutoGlobalScope());
            globalScope.Push(declarableGlobalScope);

            var astConverter = new ASTConverter(globalScope);
            astConverter.AddModule(syntaxModule);
            astConverter.Convert();

            var output = astConverter.GetModule(syntaxModule);

            CodeGenerator.GenerateIL(output);
        }

        private static void InitializeGlobalOperators(SimpleScope scope)
        {
            // Initialize numeric types
            foreach (var type in new[] { BuiltinType.Byte, BuiltinType.Integer, BuiltinType.UnsignedInteger, BuiltinType.Real })
            {
                scope.AddOperator(PsiOperator.Plus, FunctionType.CreateUnaryOperator(type));
                scope.AddOperator(PsiOperator.Minus, FunctionType.CreateUnaryOperator(type));

                scope.AddOperator(PsiOperator.Plus, FunctionType.CreateBinaryOperator(type));
                scope.AddOperator(PsiOperator.Minus, FunctionType.CreateBinaryOperator(type));
                scope.AddOperator(PsiOperator.Multiply, FunctionType.CreateBinaryOperator(type));
                scope.AddOperator(PsiOperator.Divide, FunctionType.CreateBinaryOperator(type));
                scope.AddOperator(PsiOperator.Modulo, FunctionType.CreateBinaryOperator(type));

                scope.AddOperator(PsiOperator.Equals, FunctionType.CreateBinaryOperator(BuiltinType.Boolean, type));
                scope.AddOperator(PsiOperator.NotEquals, FunctionType.CreateBinaryOperator(BuiltinType.Boolean, type));
                scope.AddOperator(PsiOperator.Less, FunctionType.CreateBinaryOperator(BuiltinType.Boolean, type));
                scope.AddOperator(PsiOperator.LessOrEqual, FunctionType.CreateBinaryOperator(BuiltinType.Boolean, type));
                scope.AddOperator(PsiOperator.More, FunctionType.CreateBinaryOperator(BuiltinType.Boolean, type));
                scope.AddOperator(PsiOperator.MoreOrEqual, FunctionType.CreateBinaryOperator(BuiltinType.Boolean, type));
            }

            // Initialize integral types
            foreach (var type in new[] { BuiltinType.Byte, BuiltinType.Integer, BuiltinType.UnsignedInteger })
            {
                scope.AddOperator(PsiOperator.Invert, FunctionType.CreateUnaryOperator(type));

                scope.AddOperator(PsiOperator.And, FunctionType.CreateBinaryOperator(type));
                scope.AddOperator(PsiOperator.Or, FunctionType.CreateBinaryOperator(type));
                scope.AddOperator(PsiOperator.Xor, FunctionType.CreateBinaryOperator(type));
                scope.AddOperator(PsiOperator.ShiftRight, FunctionType.CreateBinaryOperator(type));
                scope.AddOperator(PsiOperator.ArithmeticShiftRight, FunctionType.CreateBinaryOperator(type));
            }

            // Initialize real type
            {
                scope.AddOperator(PsiOperator.Exponentiate, FunctionType.CreateBinaryOperator(BuiltinType.Real));
            }

            // Initialize boolean type
            {
                scope.AddOperator(PsiOperator.Invert, FunctionType.CreateUnaryOperator(BuiltinType.Boolean));
                scope.AddOperator(PsiOperator.And, FunctionType.CreateBinaryOperator(BuiltinType.Boolean));
                scope.AddOperator(PsiOperator.Or, FunctionType.CreateBinaryOperator(BuiltinType.Boolean));
                scope.AddOperator(PsiOperator.Xor, FunctionType.CreateBinaryOperator(BuiltinType.Boolean));
            }
        }

        private static Grammar.Module Load(string fileName)
        {
            using (var lexer = new PsiLexer(fileName))
            {
                var parser = new PsiParser(lexer);

                var success = parser.Parse();

                if (success)
                    return parser.Result;
                Console.WriteLine("Line: {0}", lexer.yylloc.StartLine);
                return null;
            }
        }

        private static Intermediate.Module CreateStd()
        {
            var std = new Intermediate.Module(null, "std");
            std.AddType("bool", BuiltinType.Boolean, true);
            std.AddType("byte", BuiltinType.Byte, true);
            std.AddType("int", BuiltinType.Integer, true);
            std.AddType("uint", BuiltinType.UnsignedInteger, true);
            std.AddType("char", BuiltinType.Character, true);
            std.AddType("real", BuiltinType.Real, true);
            std.AddType("string", BuiltinType.String, true);

            std.AddConst("true", BuiltinType.Boolean, new BoolLiteral(true), true);
            std.AddConst("false", BuiltinType.Boolean, new BoolLiteral(false), true);

            var compiler = new Intermediate.Module(std, "compiler");
            compiler.AddType("type", Type.MetaType, true);
            compiler.AddType("void", Type.VoidType, true);
            compiler.AddType("module", Type.ModuleType, true);
            std.AddModule("compiler", compiler);

            var math = new Intermediate.Module(std, "math");
            math.AddConst("pi", BuiltinType.Real, new RealLiteral(3.14159265358979323846), true);
            math.AddConst("e", BuiltinType.Real, new RealLiteral(2.71828182845904523536), true);
            std.AddModule("math", math);

            /*
            var typelist = new Type[] {
                BuiltinType.Character,
                BuiltinType.String,
                BuiltinType.Real,
                BuiltinType.UnsignedInteger,
                BuiltinType.Byte,
                BuiltinType.Boolean,
                BuiltinType.Integer
            };
            foreach (var type in typelist)
                io.AddExtern(new FunctionType(Type.VoidType, type), "print");
            */
            { // Input/Output Module
                var io = new Intermediate.Module(std, "io");

                var mod = CodeGenerator.module;
                var putchar = LLVM.AddFunction(mod, "putchar", LLVM.FunctionType(LLVM.VoidType(), new[] { LLVM.Int32Type() }, false));
                LLVM.SetLinkage(putchar, LLVMLinkage.LLVMExternalLinkage);

                var printfun = LLVM.AddFunction(mod, "print(byte)", LLVM.FunctionType(LLVM.VoidType(), new[] { LLVM.Int8Type() }, false));
                LLVM.SetLinkage(printfun, LLVMLinkage.LLVMExternalLinkage);

                var builder = LLVM.CreateBuilder();

                // Create a new basic block to start insertion into.
                LLVM.PositionBuilderAtEnd(builder, LLVM.AppendBasicBlock(printfun, "entry"));

                var arg = LLVM.BuildZExt(builder, LLVM.GetParam(printfun, 0), LLVM.Int32Type(), "c");

                LLVM.BuildCall (builder, putchar, new[] { arg }, "");

                // Finish off the function with a default return value
                LLVM.BuildRetVoid(builder);

                LLVM.DisposeBuilder(builder);

                // Validate the generated code, checking for consistency.
                LLVM.VerifyFunction(printfun, LLVMVerifierFailureAction.LLVMPrintMessageAction);

                io.AddExtern("print", new BuiltinFunction(new FunctionType(Type.VoidType, BuiltinType.Byte), printfun));

                std.AddModule("io", io);
            }

            return std;
        }
    }

    static class ScopeExt
    {
        public static Symbol AddBuiltin(this Intermediate.Module mod, FunctionType type, string name)
        {
            var sym = new Symbol(type, name)
            {
                IsConst = true,
                IsExported = false,
                Kind = SymbolKind.Builtin,
                Initializer = new Intermediate.FunctionLiteral(new BuiltinFunction(type)),
            };
            mod.Symbols.Add(sym);
            return sym;
        }

        public static Symbol AddExtern(this Intermediate.Module mod, string name, BuiltinFunction value)
        {
            var sym = new Symbol(value.Type, name)
            {
                IsConst = true,
                IsExported = false,
                Kind = SymbolKind.Extern,
                Initializer = new Intermediate.FunctionLiteral(value),
            };
            mod.Symbols.Add(sym);
            return sym;
        }

        public static Symbol AddOperator(this SimpleScope scope, PsiOperator op, FunctionType type)
        {
            var sym = new Symbol(type, op)
            {
                Initializer = new Intermediate.FunctionLiteral(new BuiltinFunction(type)),
                IsConst = true,
                IsExported = false,
                Kind = SymbolKind.Builtin
            };
            scope.Add(sym);
            return sym;
        }
    }
}
