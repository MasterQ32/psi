﻿using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace midend
{
	using midend.AbstractSyntaxTree;

	public static class AST
	{
		public static Program Load(TextReader reader)
		{
			var ser = new XmlSerializer(typeof(Program));
			ser.UnknownElement += (s, e) =>
			{
				Console.Error.WriteLine("Unknown node: {0}.{1}", e.ObjectBeingDeserialized?.GetType()?.Name, e.Element?.Name);
			};
			return (Program)ser.Deserialize(reader);
		}

		public static void Store(TextWriter writer, Program root)
		{
			var ser = new XmlSerializer(typeof(Program));
			ser.Serialize(writer, root);
		}
	}

	namespace AbstractSyntaxTree
	{
		using System.Xml.Serialization;

		[XmlRoot("program")]
		public sealed class Program
		{
			[XmlElement("import")]
			public Import[] Imports { get; set; }

			[XmlElement("vardecl")]
			public VariableDeclaration[] Variables { get; set; }

			[XmlElement("operatordecl")]
			public OperatorDeclaration[] Operators { get; set; }

			[XmlElement("assertion")]
			public Assertion[] Assertions { get; set; }

			[XmlElement("module")]
			public Module[] Modules { get; set; }

			/// <summary>
			/// Creates all declared modules of this program in targetScope.
			/// </summary>
			/// <param name="targetScope">Target scope.</param>
			public midend.Module BuildModuleStructure(midend.Module containingModule, string name)
			{
				var module = containingModule.AddSubModule(name);
				if (this.Modules == null)
					return module;
				foreach (var mod in this.Modules)
				{
					if (mod.Contents == null) throw new InvalidDataException("'Empty' module found!");

					var container = module;
					for (int i = 0; i < (mod.Name.Count - 1); i++)
					{
						container = container.GetSubModule(mod.Name[i], true);
					}
					BuildModuleStructure(container, name);
				}
				return module;
			}

			/// <summary>
			/// This method gathers all symbols defined in this program including all submodules.
			/// </summary>
			/// <returns>The symbols.</returns>
			/// <param name="targetScope">Target scope.</param>
			/// <remarks>All referenced modules must already exist in <paramref name="targetScope"/>.</remarks>
			public IEnumerable<SymbolDefinition> GatherSymbols(midend.Module targetModule)
			{
				if (this.Variables != null)
				{
					foreach (var var in this.Variables)
					{
						yield return new SymbolDefinition(targetModule, var);
					}
				}
				if (this.Modules != null)
				{
					foreach (var mod in this.Modules)
					{
						var innerScope = mod.Name.Path.Aggregate(targetModule, (current, name) => current.GetSubModule(name));
						foreach (var def in mod.Contents.GatherSymbols(innerScope))
							yield return def;
					}
				}
				if (this.Operators != null)
				{
					throw new NotImplementedException("Operators are not supported yet.");
				}
			}

			public IEnumerable<midend.Assertion> CreateAllAssertions(midend.Module module)
			{
				if (this.Assertions != null)
				{
					var context = new ResolvationContext(module.Scope);
					foreach (var assert in this.Assertions)
					{
						var claim = assert.Claim.TryResolve(context);
						yield return module.AddAssertion(claim.Simplify());
					}
				}
				if (this.Modules != null)
				{
					foreach (var mod in this.Modules)
					{
						var innerScope = mod.Name.Path.Aggregate(module, (current, name) => current.GetSubModule(name));
						foreach (var assert in mod.Contents.CreateAllAssertions(innerScope))
						{
							yield return assert;
						}
					}
				}
			}
		}

		public sealed class Module
		{
			[XmlElement("name")]
			public SymbolPath Name { get; set; }

			[XmlElement("contents")]
			public Program Contents { get; set; }
		}

		public sealed class Assertion : BodyContent
		{
			[XmlElement("expression")]
			public AbstractExpression Claim { get; set; }
		}

		public sealed class Param
		{
			[XmlElement("name")]
			public string Name { get; set; }

			[XmlElement("type")]
			public AbstractType Type { get; set; }

			[XmlElement("value")]
			public AbstractExpression Value { get; set; }
		}

		public sealed class Import
		{
			[XmlElement("module")]
			public SymbolPath Module { get; set; }

			[XmlElement("alias")]
			public string Alias { get; set; }
		}

		public sealed class OperatorDeclaration : BodyContent
		{
			[XmlElement("operatortype")]
			public OperatorType Type { get; set; }

			[XmlElement("operator")]
			public Operator Operator { get; set; }

			[XmlElement("func")]
			public ExpressionFunction Function { get; set; }

			[XmlElement("isExported")]
			public bool IsExported { get; set; }
		}

		public sealed class VariableDeclaration : BodyContent
		{
			[XmlElement("type")]
			public AbstractType Type { get; set; }

			[XmlElement("value")]
			public AbstractExpression Value { get; set; }

			[XmlArray("params"), XmlArrayItem("param")]
			public Param[] GenericArguments { get; set; }

			[XmlElement("name")]
			public string Name { get; set; }

			[XmlElement("isConst")]
			public bool IsConstant { get; set; }

			[XmlElement("isExported")]
			public bool IsExported { get; set; }

			[XmlElement("isGeneric")]
			public bool IsGeneric { get; set; }

			// TODO: Implement generic declarations!
		}

		#region Types

		[XmlInclude(typeof(TypeReference))]
		[XmlInclude(typeof(TypeFunction))]
		[XmlInclude(typeof(TypeEnum))]
		[XmlInclude(typeof(TypeRecord))]
		public abstract class AbstractType
		{
			public abstract CType TryResolve(ResolvationContext context);
		}

		public sealed class TypeReference : AbstractType
		{
			[XmlElement("name")]
			public SymbolPath Name { get; set; }

			[XmlArray("args"), XmlArrayItem("expression")]
			public AbstractExpression[] Arguments { get; set; }

			public override CType TryResolve(ResolvationContext context)
			{
				if (this.Arguments != null)
					throw new NotSupportedException("Generics are not supported yet!");

				var scope = context.Scope;
				for (int i = 0; i < (this.Name.Count - 1); i++)
				{
					var sym = scope[this.Name[i], CTypes.Module];
					if (sym == null)
						return null;
					var mod = (Scope)sym.InitialValue.Evaluate(null).Value;
					if (mod == null)
						return null;
					scope = mod;
				}

				var type = scope[this.Name.LocalName, CTypes.Type];
				if (type == null)
					return null;
				if (type.HasStaticValue == false)
					return null;
				return (CType)type.InitialValue.Evaluate(null).Value;
			}
		}

		public sealed class TypeFunction : AbstractType
		{
			[XmlArray("params"), XmlArrayItem("param")]
			public Param[] Parameters { get; set; }

			[XmlElement("returntype")]
			public AbstractType ReturnType { get; set; }

			[XmlArray("restrictions"), XmlArrayItem("expression")]
			public AbstractExpression[] Restrictions { get; set; }
			
			public override CType TryResolve(ResolvationContext context)
			{
				throw new NotImplementedException("Function type is not implemented yet.");
			}
		}

		public sealed class TypeEnum : AbstractType
		{
			[XmlArray("members"), XmlArrayItem("string")]
			public string[] Members { get; set; }

			public override CType TryResolve(ResolvationContext context)
			{
				var members = new HashSet<string>(this.Members);
				if (members.Count != this.Members.Length)
					throw new IndexOutOfRangeException("Duplicate enum members!");
				return new EnumType(members);
			}
		}

		public sealed class TypeRecord : AbstractType
		{
			[XmlArray("fields"), XmlArrayItem("param")]
			public Param[] Fields { get; set; }

			public override CType TryResolve(ResolvationContext context)
			{
				var members = new RecordMember[this.Fields.Length];
				for (int i = 0; i < members.Length; i++)
				{
					CValue initial = null;
					var name = this.Fields[i].Name;
					var type = this.Fields[i].Type.TryResolve(context);
					if (type == null)
						return null;
					if (this.Fields[i].Value != null)
					{
						initial = this.Fields[i].Value.TryResolve(context)?.Execute();
						if (initial == null)
							return null;
					}
					members[i] = new RecordMember(type, name, initial);
				}

				return new RecordType(members);
			}
		}

		#endregion

		#region Expression

		[XmlInclude(typeof(ExpressionNumber))]
		[XmlInclude(typeof(ExpressionString))]
		[XmlInclude(typeof(ExpressionType))]
		[XmlInclude(typeof(ExpressionBinaryOperator))]
		[XmlInclude(typeof(ExpressionUnaryOperator))]
		[XmlInclude(typeof(ExpressionSymbol))]
		[XmlInclude(typeof(ExpressionFunction))]
		[XmlInclude(typeof(ExpressionIndex))]
		[XmlInclude(typeof(ExpressionArray))]
		[XmlInclude(typeof(ExpressionNew))]
		public abstract class AbstractExpression
		{
			public virtual Expression TryResolve(ResolvationContext context)
			{
				throw new NotImplementedException($"Expression translation not implemented for {this.GetType().Name}!");
			}
		}

		public sealed class ExpressionArray : AbstractExpression
		{
			[XmlArray("values"), XmlArrayItem("expression")]
			public AbstractExpression[] Items { get; set; }

			public override Expression TryResolve(ResolvationContext context)
			{
				var items = this.Items.Select(exp => exp.TryResolve(context)?.Simplify()).ToArray();
				if (items.Any(i => (i == null)))
					return null;
				return new ArrayExpression(items);
			}
		}

		public sealed class ExpressionNumber : AbstractExpression
		{
			[XmlElement("value")]
			public string Value { get; set; }

			// TODO: Implement real values as well
			public override Expression TryResolve(ResolvationContext context) => Expression.Constant(BigInteger.Parse(this.Value));
		}

		public sealed class ExpressionString : AbstractExpression
		{
			[XmlElement("value")]
			public string Value { get; set; }

			public override Expression TryResolve(ResolvationContext context) => Expression.Constant(this.Value);
		}

		public sealed class ExpressionType : AbstractExpression
		{
			[XmlElement("reference")]
			public AbstractType Type { get; set; }

			public override Expression TryResolve(ResolvationContext context)
			{
				var type = this.Type.TryResolve(context);
				if (type == null)
					return null;
				return Expression.Constant(type);
			}
		}

		public sealed class ExpressionSymbol : AbstractExpression
		{
			[XmlElement("name")]
			public string Symbol { get; set; }

			public override Expression TryResolve(ResolvationContext context)
			{
				var symbol = context.GetSymbol(this.Symbol);
				if(symbol == null)
					return null;
				return new SymbolReferenceExpression(symbol);
				/*
				var symbols = targetScope.GetAll(this.Symbol);
				if (symbols.Length == 0)
					return null; // Empty
				if (symbols.Length != 1)
				{
					// TODO: Implement polymorphic variables!
					throw new NotImplementedException("There is something missing....");
				}
				return new SymbolReferenceExpression(targetScope[symbols[0]]);
				*/
			}
		}

		public sealed class ExpressionBinaryOperator : AbstractExpression
		{
			[XmlElement("lhs")]
			public AbstractExpression LeftHandSide { get; set; }

			[XmlElement("rhs")]
			public AbstractExpression RightHandSide { get; set; }

			[XmlElement("operator")]
			public Operator Operator { get; set; }

			public override Expression TryResolve(ResolvationContext context)
			{
				var lhs = this.LeftHandSide.TryResolve(context);
				var rhs = this.RightHandSide.TryResolve(context);

				if (lhs == null || rhs == null)
					return null;

				var opsyms = context
					.Scope
					.GetAll(this.Operator)
					.Select(sig => context.Scope[sig])
					.Where(sym => sym.Type is BinaryOperatorType)
					.ToArray();
				if (opsyms == null || opsyms.Length == 0)
					return null;

				var @operator = opsyms.First(o =>
					lhs.Type.CanBeAssignedTo(((BinaryOperatorType)o.Type).LeftHandSide)
					&& rhs.Type.CanBeAssignedTo(((BinaryOperatorType)o.Type).RightHandSide));

				return new FunctionCallExpression(@operator.InitialValue.Simplify(), lhs.Simplify(), rhs.Simplify());
			}
		}

		public sealed class ExpressionUnaryOperator : AbstractExpression
		{
			[XmlElement("value")]
			public AbstractExpression Value { get; set; }

			[XmlElement("operator")]
			public Operator Operator { get; set; }

			public override Expression TryResolve(ResolvationContext context)
			{
				var value = this.Value.TryResolve(context);

				if (value == null)
					return null;

				var opsyms = context
					.Scope
					.GetAll(this.Operator)
					.Select(sig => context.Scope[sig])
					.Where(sym => sym.Type is UnaryOperatorType)
					.ToArray();
				if (opsyms == null || opsyms.Length == 0)
					return null;

				var @operator = opsyms.First(o => value.Type.CanBeAssignedTo(((UnaryOperatorType)o.Type).Value));

				return new FunctionCallExpression(@operator.InitialValue.Simplify(), value.Simplify());
			}
		}

		public sealed class ExpressionFunction : AbstractExpression
		{
			[XmlElement("signature")]
			public TypeFunction Signature { get; set; }

			[XmlElement("body")]
			public AbstractInstruction Body { get; set; }
		}

		public sealed class ExpressionIndex : AbstractExpression
		{
			[XmlElement("value")]
			public AbstractExpression Value { get; set; }

			[XmlElement("index")]
			public AbstractIndex Index { get; set; }

			public override Expression TryResolve(ResolvationContext context)
			{
				var value = Value?.TryResolve(context);
				if (value == null)
				{
					return null;
				}

				if (Index is IndexField)
				{
					var index = (IndexField)Index;
					if (value.Type is ModuleType)
					{ // Special case: Module declarations need "dynamic" indexing at compile time
						if (!value.IsConstant) throw new InvalidOperationException("This should not happen...");
						var module = (midend.Module)value.Evaluate(null).Value;
						var options = module.Scope.Locals.Where(l => l.Name == index.Field).ToArray();
						if (options.Length != 1) throw new InvalidOperationException("Symbol has multiple definitions or is not declared!");

						return new SymbolReferenceExpression(module.Scope[options[0]]);
					}
					else
					{
						var field = value.Type.GetField(index.Field);
						if (field == null)
						{
							if (value.IsConstant)
							{
								var contents = value.Evaluate(null);
								field = contents.GetField(index.Field);
							}
						}
						if (field == null)
						{
							throw new InvalidOperationException($"Value field `.{index.Field}` does not exist!");
						}
						return new FieldIndexExpression(value.Simplify(), field);
					}
				}
				else if (Index is IndexMeta)
				{
					var index = (IndexMeta)Index;
					var field = value.Type.GetMetaField(index.Field);
					if (field == null) throw new InvalidOperationException($"Meta field `'{index.Field}` does not exist!");
					return new FieldIndexExpression(value.Simplify(), field);
				}
				else if (Index is IndexCall)
				{
					var index = (IndexCall)Index;

					var args = index.Arguments.Select(arg => arg.TryResolve(context)).ToArray();
					if (args.Any(a => (a == null)))
						return null;

					return new FunctionCallExpression(value, args);
				}
				else if (Index is IndexArray)
				{
					var index = (IndexArray)Index;
					var indexes = new Expression[index.Indices.Length];
					for (int i = 0; i < indexes.Length; i++)
					{
						indexes[i] = index.Indices[i].TryResolve(context)?.Simplify();
						if (indexes[i] == null)
						{
							return null;
						}
					}
					return new ArrayIndexExpression(value, indexes);
				}
				throw new NotSupportedException();
			}
		}

		public sealed class ExpressionNew : AbstractExpression
		{
			[XmlElement("recordtype")]
			public SymbolPath RecordTypeName { get; set; }

			[XmlArray("arguments"), XmlArrayItem("argument")]
			public AbstractArgument[] Arguments { get; set; }
		}

		#endregion

		#region Indices

		[XmlInclude(typeof(IndexArray))]
		[XmlInclude(typeof(IndexField))]
		[XmlInclude(typeof(IndexMeta))]
		[XmlInclude(typeof(IndexCall))]
		public abstract class AbstractIndex
		{

		}

		public sealed class IndexArray : AbstractIndex
		{
			[XmlArray("indices"), XmlArrayItem("expression")]
			public AbstractExpression[] Indices { get; set; }
		}

		public sealed class IndexField : AbstractIndex
		{
			[XmlElement("field")]
			public string Field { get; set; }
		}

		public sealed class IndexMeta : AbstractIndex
		{
			[XmlElement("field")]
			public string Field { get; set; }
		}

		public sealed class IndexCall : AbstractIndex
		{
			[XmlArray("arguments"), XmlArrayItem("argument")]
			public AbstractArgument[] Arguments { get; set; }
		}

		[XmlInclude(typeof(ArgumentPositional))]
		[XmlInclude(typeof(ArgumentNamed))]
		public abstract class AbstractArgument
		{
			[XmlElement("value")]
			public AbstractExpression Value { get; set; }

			public abstract Argument TryResolve(ResolvationContext context);
		}

		public sealed class ArgumentPositional : AbstractArgument
		{
			[XmlElement("position")]
			public int Position { get; set; }

			public override Argument TryResolve(ResolvationContext context)
			{
				var value = this.Value.TryResolve(context);
				if (value == null) return null;
				return new PositionalArgument(this.Position - 1, value);
			}
		}

		public sealed class ArgumentNamed : AbstractArgument
		{
			[XmlElement("name")]
			public string Name { get; set; }

			public override Argument TryResolve(ResolvationContext context)
			{
				var value = this.Value.TryResolve(context);
				if (value == null) return null;
				return new NamedArgument(this.Name, value);
			}
		}

		#endregion

		#region Instructions

		[XmlInclude(typeof(InstructionBody))]
		[XmlInclude(typeof(InstructionExpression))]
		[XmlInclude(typeof(InstructionConditional))]
		[XmlInclude(typeof(InstructionReturn))]
		[XmlInclude(typeof(InstructionWhile))]
		[XmlInclude(typeof(InstructionLoopUntil))]
		[XmlInclude(typeof(InstructionFor))]
		[XmlInclude(typeof(InstructionEmpty))]
		[XmlInclude(typeof(InstructionDelete))]
		[XmlInclude(typeof(InstructionRestriction))]
		[XmlInclude(typeof(InstructionBreak))]
		[XmlInclude(typeof(InstructionContinue))]
		[XmlInclude(typeof(InstructionNext))]
		[XmlInclude(typeof(InstructionGoto))]
		[XmlInclude(typeof(InstructionSelect))]
		public abstract class AbstractInstruction : BodyContent
		{

		}

		public sealed class InstructionEmpty : AbstractInstruction
		{
		}

		public sealed class InstructionBreak : AbstractInstruction
		{
		}

		public sealed class InstructionContinue : AbstractInstruction
		{
		}

		public sealed class InstructionNext : AbstractInstruction
		{
		}

		public sealed class InstructionGoto : AbstractInstruction
		{
			[XmlElement("expression")]
			public AbstractExpression Target { get; set; }
		}

		public sealed class InstructionSelect : AbstractInstruction
		{
			[XmlElement("selector")]
			public AbstractExpression Selector { get; set; }

			[XmlArray("options"), XmlArrayItem("selector")]
			public SelectOption[] Options { get; set; }
		}

		public sealed class SelectOption
		{
			[XmlElement("isDefault")]
			public bool IsDefault { get; set; }

			[XmlElement("expression")]
			public AbstractExpression Value { get; set; }

			[XmlElement("contents")]
			public InstructionBody Contents { get; set; }
		}

		public sealed class InstructionBody : AbstractInstruction
		{
			[XmlElement("instruction", typeof(AbstractInstruction))]
			[XmlElement("vardecl", typeof(VariableDeclaration))]
			public BodyContent[] Contents { get; set; }

			// public VariableDeclaration[] Variables { get; set; }
		}

		[XmlInclude(typeof(AbstractInstruction))]
		[XmlInclude(typeof(VariableDeclaration))]
		[XmlInclude(typeof(Assertion))]
		public abstract class BodyContent
		{

		}

		public sealed class InstructionExpression : AbstractInstruction
		{
			[XmlElement("expression")]
			public AbstractExpression Expression { get; set; }
		}

		public sealed class InstructionWhile : AbstractInstruction
		{
			[XmlElement("condition")]
			public AbstractExpression Expression { get; set; }

			[XmlElement("body")]
			public AbstractInstruction Body { get; set; }
		}

		public sealed class InstructionLoopUntil : AbstractInstruction
		{
			[XmlElement("condition")]
			public AbstractExpression Expression { get; set; }

			[XmlElement("body")]
			public AbstractInstruction Body { get; set; }
		}

		public sealed class InstructionConditional : AbstractInstruction
		{
			[XmlElement("condition")]
			public AbstractExpression Condition { get; set; }

			[XmlElement("positive")]
			public AbstractInstruction Positive { get; set; }

			[XmlElement("negative")]
			public AbstractInstruction Negative { get; set; }
		}

		public sealed class InstructionReturn : AbstractInstruction
		{
			[XmlElement("expression")]
			public AbstractExpression Value { get; set; }
		}

		public sealed class InstructionDelete : AbstractInstruction
		{
			[XmlElement("value")]
			public AbstractExpression Value { get; set; }
		}

		public sealed class InstructionRestriction : AbstractInstruction
		{
			[XmlArray("restrictions"), XmlArrayItem("expression")]
			public ExpressionBinaryOperator[] Restrictions { get; set; }

			[XmlElement("success")]
			public AbstractInstruction Success { get; set; }

			[XmlElement("failure")]
			public AbstractInstruction Failure { get; set; }
		}

		public sealed class InstructionFor : AbstractInstruction
		{
			[XmlElement("variable")]
			public string Variable { get; set; }

			[XmlElement("expression")]
			public AbstractExpression Value { get; set; }

			[XmlElement("vartype")]
			public AbstractType Type { get; set; }

			[XmlElement("body")]
			public AbstractInstruction Body { get; set; }
		}

		#endregion
	}
}
