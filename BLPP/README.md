# Blockland Preprocessor (0.4.0)

This program transpiles custom preprocessor directives into TorqueScript code. It mainly allows for the usage of macros, like in C/C++.

It's mainly targetted towards Blockland, but it can be used for any software that runs on TGE 1.3 or before.

## Basics

The Blockland Preprocessor ***does not*** read `.cs` files! It reads `.blcs` files instead.

All files must start with the `##blcs` directive.

## Macros

You define macros with `##define` like so:

```cs
##define MAX_INT_VALUE 999999
```

You then use macros like this:

```js
if ($someVar > #MAX_INT_VALUE)
{
    error("Max integer value reached!");
}
```

### Multiline Macros

Multiline macros are supported with brackets `#{ ... #}` like so:

```cs
##define myMultilineMacro
#{
    echo("line 1");
    echo("line 2");
    echo("line 3");
    echo("line 4");
#}
```

### Arguments

You can define macros that take in arguments. To use arguments in your macro, prefix them with `#%`:

```cs
##define add(num1, num2) #%num1 + #%num2
```

Then use them like so:

```cs
return #add(1, 2);
```

The above will expand to:

```cs
return 1 + 2;
```

If you want to surround your macro with parentheses you'll have to surround them with brackets so as to not confuse the parser (or yourself):

```cs
##define myParenthesizedMacro #{ (getRandom(1, 100) * getRandom(1, 100)) #}
```

### Variadic Macros

Use `...` when defining a macro to declare it as variadic (it *must* be the last "parameter"), and use `#!vargs` to insert said arguments into your macro:

```cs
##define error(errorCode, ...)
#{
    $LastError = #%errorCode;
    error("ERROR: ", #!vargs);
#}

#error(0xD34DB33F, "Failed to reticulate splines!", "Very Not Good.");
```

The above will expand to:

```cs
$LastError = 0xD34DB33F; error("ERROR: ", "Failed to reticulate splines!", "Very Not Good.");
```

To prepend a comma before inserting the arguments, use `#!vargsp`. If no variadic arguments are passed in, no comma will be inserted.

You can also insert the number of arguments passed in with `#!vargc`.

### Argument Concatenation

Use `#@` for macro argument concatenation.

```cs
##define DefineEnemyDataBlock(name, health, silly)
#{
datablock PlayerData(Enemy_ #@ #%name)
{
    health = #%health;
    isSilly = #%silly;
};
#}

#DefineEnemyDataBlock(Orc, 50, true)
```

The above will expand to:

```cs
datablock PlayerData(Enemy_Orc) { health = 50; isSilly = true; };
```

### String Literal Concatenation

You can also use `#@` for string literal concatenation, ***as long as they use the same quote character***.

For example:

```cs
##define concat(str1, str2) #%str1 #@ #%str2

echo(#concat("Hello ", "there!"));
```

The above will expand to:

```cs
echo("Hello there!");
```

The same will work for two tagged strings.

However, if you try to use a tagged string and a regular string, it ***will not*** work:

```cs
echo(#concat('Hello ', "there!"));
```

The above will expand to:

```cs
echo('Hello '"there!");
```

### Using Macros from Other Files

To use macros from other files, you can do so like this:

```cs
##use "relative/path/to/file.blcs"
```

Please note that ***this does not execute the file***—it simply processes the file and makes the macro definitions available.

### Miscellaneous

You can insert the line number into your macros with `#!line`.

If you want to insert it elsewhere into your code, you can just `##define line #!line` and then use `#line` anywhere.

## Usage

There are two ways to use this program: either as a typical console program, or as a command-line interface.

To use it normally, just drag a `.blcs` onto the program. Make sure any imported macro files are in the correct folders.

You can also use it as a command-line interface: `usage: BLPP path [-h] [-d] (-w | -X) [-q] [-e]`

`-h`, `--help`  Displays help.

`-d`, `--directory` Specifies that the path is a directory.

`-w`, `--watch` Watches a directory for changes, and automatically processes any files that were changed.

`-q`, `--quiet` Disables all messages (except command-line argument errors).

`-e`, `--output-empty` Forces creation of processed files that are empty.

`-X`, `--cli` Makes the program operate as a command-line interface that takes no keyboard input and closes immediately upon completion or failure. (Incompatible with `--watch`)

## Building

### Windows

To build for Windows:

1. Open in Visual Studio 2022 (or later)
2. Right-click on the `BLPP` project and click "Publish"
3. Create a new profile with the "Folder" target
4. Set the "Target Runtime" to `win-x64`
5. Click "Show all settings" and set "Deployment mode" to `Self-contained`
6. Click "Save" and then click the large "Publish" button in the top right corner

### Linux

To build for Linux:

1. Install the .NET 8.0 SDK with `sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0`
2. Navigate to the repo folder
3. Build the project: `dotnet publish -a x64 --os linux -c Release --sc`

### macOS

To build for macOS:

1. Don't.
