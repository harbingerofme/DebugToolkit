# Command Autocomplete

## About

Command Autocomplete is an enhancement by DebugToolkit which provides autofill options for any argument in a command.

It also features partial string highlighting and allows multiple strings as aliases for the same option. The match ordering will take into consideration the closest match for each option.

## Usage

Pair `AutoCompleteAttribute` with any `ConCommandAttribute`s in your mod and then use `AutoCompleteParser` to register the autocompletion options.

### Attribute
The string in the attribute is scanned for any tokens containing brackets, .e.g., `{}`, `[]`, and `<>`, which are used to define what options are available for each argument. The pattern within these brackets falls under two categories:
- `variable`, where variable defines the collection of strings available.
- `(variable|'string')`, where the options in the round brackets are separated by a pipe (`|`). Tokens surrounded by single quotes are taken as literal strings.

Use the latter for explicit options or seemingly unrelated ones where a variable name can't provide an easy description.

If a colon (`:`) appears in the argument definition, anything to its right will be ignored. This can be used to convey any additional information to the user, e.g., a default value or input type.

Any of the following are valid examples (assume `x` represents the list of values "1", "2", "3"):
- `{x}` - The options will be ["1", "2", "3"]
- `[x:1]` - Same as above
- `<argument_name (x|'something'):(int|string)>` - ["1", "2", "3", "something"]

### Variables

A **static variable** is a shorthand for a fixed collection of strings, e.g., items, true/false, etc.

**Dynamic variables** represent collections whose contents may change during runtime, e.g., number of players. These can be constructed as and when needed using reflection. Simply provide the collection object along with any futher nested field(s) if needed concatenated with `"/"`. For example `(list, "a/b/c")` is the same as accessing `list[i].a.b.c`. The optional argument `"isToken"` decides whether the resulting value will be matched against any language tokens.

Each string in a variable can contain multiple values concatenated with `"|"`, which point to the same selection. This is useful for enum values, e.g., `"0|This"`, `"1|That"`, etc. For a dynamic variable use the optional argument `"showIndex"` to toggle this behavior.

While scanning the assembly any unregistered variables encountered will be ignored. This is useful for inputs where autofill options would be counterproductive or unnecessary, e.g., all integers.

### Example

```
private void Awake()
{
    RoR2Application.onLoad += delegate()
    {
        var parser = new AutoCompleteParser();
        parser.RegisterStaticVariable("action", new string[] { "1", "2", "3" });
        parser.RegisterDynamicVariable("player", NetworkUser.instancesList, "userName");
        parser.ScanAssembly();
    };
}

[ConCommand(...)]
[AutoComplete("[action] [times:1] [(player|'everyone'|'self')]")]
private static void CCDoSomething(ConCommandArgs args) { }
```