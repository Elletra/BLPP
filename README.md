# Blockland Preprocessor

This program transpiles custom preprocessor directives into TorqueScript code. It mainly allows for the usage of macros, like in C/C++.

It's mainly targetted towards Blockland, but it can be used for any software that runs on the TGE 1.3 or before.

## Basics

The Blockland Preprocessor ***does not*** read `.cs` files! It reads `.blcs` files instead.

All files must start with `##blcs`.

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

### Using Macros from Other Files

To use macros from other files, you can do so like this:

```cs
##use "relative/path/to/file.blcs"
```

Please note that **this does not execute the file**—it simply processes the file and makes the macro definitions available to your file.

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

`-X`, `--cli` Makes the program operate as a command-line interface that takes no keyboard input and closes immediately upon completion or failure. (Incompatible with `--watch`).
