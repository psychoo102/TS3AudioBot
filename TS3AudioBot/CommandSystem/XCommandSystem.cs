namespace TS3AudioBot.CommandSystem
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class XCommandSystem
	{
		public static readonly CommandResultType[] AllTypes = Enum.GetValues(typeof(CommandResultType)).OfType<CommandResultType>().ToArray();

		public RootCommand RootCommand { get; }

		public XCommandSystem()
		{
			RootCommand = new RootCommand();
		}

		public static IEnumerable<KeyValuePair<string, T>> FilterList<T>(IEnumerable<KeyValuePair<string, T>> list, string filter)
		{
			// Convert result to list because it can be enumerated multiple times
			var possibilities = list.Select(t => new FilterItem<T>(t.Key, t.Value, 0)).ToList();
			// Filter matching commands
			foreach (var c in filter)
			{
				var newPossibilities = (from p in possibilities
										let pos = p.name.IndexOf(c, p.index)
										where pos != -1
										select new FilterItem<T>(p.name, p.value, pos + 1)).ToList();
				if (newPossibilities.Any())
					possibilities = newPossibilities;
			}
			// Take command with lowest index
			int minIndex = possibilities.Min(t => t.index);
			var cmds = possibilities.Where(t => t.index == minIndex);
			// Take the smallest command
			int minLength = cmds.Min(c => c.name.Length);

			return cmds.Where(c => c.name.Length == minLength).Select(fi => new KeyValuePair<string, T>(fi.name, fi.value));
		}

		class FilterItem<T>
		{
			public string name;
			public T value;
			public int index;

			public FilterItem(string n, T v, int i)
			{
				name = n;
				value = v;
				index = i;
			}
		}

		internal ICommand AstToCommandResult(ASTNode node)
		{
			switch (node.Type)
			{
			case ASTType.Error:
				throw new CommandException("Found an unconvertable ASTNode of type Error");
			case ASTType.Command:
				var cmd = (ASTCommand)node;
				var arguments = new List<ICommand>();
				arguments.AddRange(cmd.Parameter.Select(n => AstToCommandResult(n)));
				return new AppliedCommand(RootCommand, arguments);
			case ASTType.Value:
				return new StringCommand(((ASTValue)node).Value);
			}
			throw new NotSupportedException("Seems like there's a new NodeType, this code should not be reached");
		}

		public ICommandResult Execute(ExecutionInformation info, string command)
		{
			return Execute(info, command, new[] { CommandResultType.String, CommandResultType.Empty });
		}

		public ICommandResult Execute(ExecutionInformation info, string command, IEnumerable<CommandResultType> returnTypes)
		{
			var ast = CommandParser.ParseCommandRequest(command);
			var cmd = AstToCommandResult(ast);
			return cmd.Execute(info, Enumerable.Empty<ICommand>(), returnTypes);
		}

		public ICommandResult Execute(ExecutionInformation info, IEnumerable<ICommand> arguments)
		{
			return Execute(info, arguments, new[] { CommandResultType.String, CommandResultType.Empty });
		}

		public ICommandResult Execute(ExecutionInformation info, IEnumerable<ICommand> arguments, IEnumerable<CommandResultType> returnTypes)
		{
			return RootCommand.Execute(info, arguments, returnTypes);
		}

		public string ExecuteCommand(ExecutionInformation info, string command)
		{
			ICommandResult result = Execute(info, command);
			if (result.ResultType == CommandResultType.String)
				return result.ToString();
			if (result.ResultType == CommandResultType.Empty)
				return null;
			throw new CommandException("Expected a string or nothing as result");
		}
	}
}
