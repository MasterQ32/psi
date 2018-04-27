﻿
using System;
using System.Collections.Generic;
using System.Linq;

namespace Psi.Compiler.Intermediate
{
    public abstract class Type : IEquatable<Type>
    {
        /// <summary>
        /// A type that marks a non-value
        /// </summary>
        public static readonly BuiltinType VoidType = new BuiltinType("void");

        /// <summary>
        /// A marker type that marks "unknown" types that have to be deduced
        /// </summary>
        public static readonly Type UnknownType = new BuiltinType("?");

        /// <summary>
        /// The type that represents a Psi type.
        /// </summary>
        public static readonly BuiltinType MetaType = new BuiltinType("type");

        /// <summary>
        /// The type that represents a module.
        /// </summary>
        public static readonly BuiltinType ModuleType = new BuiltinType("module");

        public override int GetHashCode() => base.GetHashCode();

        public override bool Equals(object obj) => Equals(obj as Type);

        public bool Equals(Type other)
        {
            if (other == null)
                return false;
            if (other.GetType() != this.GetType()) // Types have strong equality!
                return false;
            return this.TypeEquals(other);
        }

        /// <summary>
        /// Compares the two types for equality.
        /// </summary>
        /// <param name="other"></param>
        /// <remarks>other is certainly of the same type as `this`.</remarks> 
        /// <returns></returns>
        protected abstract bool TypeEquals(Type other);
    }

    public sealed class BuiltinType : Type
    {
        public static readonly EnumType Boolean = new EnumType(new[] { "false", "true" });
        public static readonly BuiltinType Byte = new BuiltinType("byte");
        public static readonly BuiltinType Integer = new BuiltinType("int");
        public static readonly BuiltinType UnsignedInteger = new BuiltinType("uint");
        public static readonly BuiltinType Real = new BuiltinType("real");
        public static readonly BuiltinType Character = new BuiltinType("char");
        public static readonly ArrayType String = new ArrayType { ElementType = Character, Dimensions = 1 };

        public BuiltinType(string name)
        {
            this.Name = name;
        }

        protected override bool TypeEquals(Type other) => object.ReferenceEquals(this, other);

        public string Name { get; }

        public override string ToString() => "builtin:" + this.Name;
    }

    public sealed class EnumType : Type
    {
        private string[] items;

        public EnumType(IReadOnlyList<string> items)
        {
            this.items = items.ToArray();
        }

        protected override bool TypeEquals(Type other)
        {
            return Enumerable.SequenceEqual(this.items, ((EnumType)other).items);
        }

        public override int GetHashCode() => this.items.Select(x => x.GetHashCode()).Aggregate(0, (a, b) => a ^ b);

        public IReadOnlyList<string> Members => this.items;

        public override string ToString() => "enum(" + string.Join(",", this.items) + ")";
    }

    public sealed class RecordType : Type
    {
        public IList<RecordMember> Members { get; set; }

        protected override bool TypeEquals(Type _other)
        {
            RecordType other = (RecordType)_other;
            return this.Members.All(m => other.Members.Contains(m))
                && other.Members.All(m => this.Members.Contains(m));
        }

        public override int GetHashCode() => this.Members.Select(x => x.GetHashCode()).Aggregate(0, (a, b) => a ^ b);

        public override string ToString() => "record(" + string.Join(", ", Members.Select(m => $"{m.Name} : {m.Type}")) + ")";
    }

    public sealed class ArrayType : Type
    {
        public Type ElementType { get; set; }

        public int Dimensions { get; set; } = 1;

        public override int GetHashCode() => ElementType.GetHashCode();

        protected override bool TypeEquals(Type other)
        {
            var at = (ArrayType)other;
            return this.Dimensions == at.Dimensions && ElementType.Equals(at.ElementType);
        }
    }

    public sealed class ReferenceType : Type
    {
        public Type ObjectType { get; set; }

        public override int GetHashCode() => -this.ObjectType.GetHashCode();

        protected override bool TypeEquals(Type other) => object.Equals(ObjectType, ((ReferenceType)other).ObjectType);
    }

    public sealed class FunctionType : Type
    {
        public Type ReturnType { get; set; }

        public IList<Parameter> Parameters { get; set; }

        protected override bool TypeEquals(Type other)
        {
            var ft = (FunctionType)other;
            if (!object.Equals(ReturnType, ft.ReturnType))
                return false;
            return Enumerable.SequenceEqual(this.Parameters, ft.Parameters);
        }

        public override int GetHashCode() =>this.ReturnType.GetHashCode() ^  this.Parameters.Select(x => x.GetHashCode()).Aggregate(0, (a, b) => a ^ b);


        public static FunctionType CreateBinaryOperator(Type allType) => CreateBinaryOperator(allType, allType, allType);

        public static FunctionType CreateBinaryOperator(Type returnType, Type paramType) => CreateBinaryOperator(returnType, paramType, paramType);

        public static FunctionType CreateBinaryOperator(Type returnType, Type leftType, Type rightType, ParameterFlags lhsFlags = ParameterFlags.In, ParameterFlags rhsFlags = ParameterFlags.In)
        {
            var fun = new FunctionType
            {
                ReturnType = returnType ?? throw new ArgumentNullException(nameof(ReturnType))
            };
            fun.Parameters = new List<Parameter>()
            {
                new Parameter(fun, "lhs", 0)
                {
                    Flags = lhsFlags,
                    Type = leftType ?? throw new ArgumentNullException(nameof(leftType)),
                    Initializer = null,
                },
                new Parameter(fun, "rhs", 1)
                {
                    Flags = rhsFlags,
                    Type = rightType ?? throw new ArgumentNullException(nameof(rightType)),
                    Initializer = null,
                }
            };
            return fun;
        }
    }
}