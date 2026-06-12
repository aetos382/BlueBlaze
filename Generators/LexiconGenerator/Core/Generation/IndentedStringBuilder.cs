using System;
using System.IO;
using System.Text;

namespace BlueBlaze.LexiconGenerator.Core.Generation;

internal sealed class IndentedStringBuilder
{
    private readonly StringBuilder _sb = new();
    private int _indentLevel;

    public IndentedStringBuilder AppendLine()
    {
        this._sb.AppendLine();
        return this;
    }

    public IndentedStringBuilder AppendLine(string text)
    {
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 0)
            {
                this._sb.Append(this.CurrentIndent);
            }

            this._sb.AppendLine(line);
        }

        return this;
    }

    public IndentScope Indent()
    {
        this._indentLevel++;
        return new IndentScope(this);
    }

    internal void Dedent()
    {
        if (this._indentLevel == 0)
        {
            throw new InvalidOperationException("Cannot dedent below zero.");
        }

        this._indentLevel--;
    }

    private string CurrentIndent => new(' ', this._indentLevel * 4);

    public override string ToString()
    {
        return this._sb.ToString();
    }

    public readonly ref struct IndentScope
    {
        private readonly IndentedStringBuilder _builder;

        internal IndentScope(IndentedStringBuilder builder)
        {
            this._builder = builder;
        }

        public readonly void Dispose()
        {
            this._builder.Dedent();
        }
    }
}
