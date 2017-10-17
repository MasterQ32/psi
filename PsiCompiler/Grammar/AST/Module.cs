﻿using System;
using System.Collections.Generic;

namespace PsiCompiler.Grammar
{
	public sealed class Module
	{		
		public Module Add(Module value)
		{
			this.Submodules.Add(value);
			return this;
		}

		public Module Add(Assertion value)
		{
			this.Assertions.Add(value);
			return this;
		}

		public Module Add(Declaration value)
		{
			this.Declarations.Add(value);
			return this;
		}
	
		public CompoundName Name { get; set; }
	
		public IList<Assertion> Assertions { get; } = new List<Assertion>();
		
		public IList<Declaration> Declarations { get; } = new List<Declaration>();
		
		public IList<Module> Submodules { get; } = new List<Module>();

		public override string ToString() => this.Name?.ToString() ?? "<unnamed module>";
	}
}