﻿using System;
namespace Psi.Compiler.Grammar
{
    // Special statement: Can be stated outside of a block.
	public sealed class Declaration : Statement
	{
		public Declaration(string name, AstType type, Expression value)
		{
			this.Name = name.NotNull();
			this.Type = type;
			this.Value = value;
		}
	
		public string Name { get; }
		
		public AstType Type { get; }
		
		public Expression Value { get; }
		
		public bool IsExported { get; set; }
		
		public bool IsConst { get; set; }

        public bool IsField { get; set; }

        public override string ToString() => string.Format(
            "{0}{1}{2} : {3} = {4}",
            IsExported ? "export " : "",
            IsField ? "" : IsConst ? "const " : "var ",
            Name,
            Type,
            Value);
    }
    
    public sealed class TypeDeclaration : Statement
	{
		public TypeDeclaration(string name, AstType type)
		{
			this.Name = name.NotNull();
			this.Type = type;
		}
	
		public string Name { get; }
		
		public AstType Type { get; }
		
		public bool IsExported { get; set; }

        public override string ToString() => string.Format(
            "{0}type {1} = {2}",
            IsExported ? "export " : "",
            Name,
            Type);
    }
}
