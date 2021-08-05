using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BL3SaveEditor.Debug {
    public class RedirectWriter : StringWriter {
        public Action<String> OnWrite;

        private void WriteGeneric<T>(T value) { if (OnWrite != null) OnWrite(value.ToString()); }

        public override void Write(char value) { WriteGeneric<char>(value); }
        public override void Write(string value) { WriteGeneric<string>(value); }
        public override void Write(bool value) { WriteGeneric<bool>(value); }
        public override void Write(int value) { WriteGeneric<int>(value); }
        public override void Write(double value) { WriteGeneric<double>(value); }
        public override void Write(long value) { WriteGeneric<long>(value); }

        private void WriteLineGeneric<T>(T value) { if (OnWrite != null) OnWrite(value.ToString() + "\n"); }
        public override void WriteLine(char value) { WriteLineGeneric<char>(value); }
        public override void WriteLine(string value) { WriteLineGeneric<string>(value); }
        public override void WriteLine(bool value) { WriteLineGeneric<bool>(value); }
        public override void WriteLine(int value) { WriteLineGeneric<int>(value); }
        public override void WriteLine(double value) { WriteLineGeneric<double>(value); }
        public override void WriteLine(long value) { WriteLineGeneric<long>(value); }

        public override void Write(char[] buffer, int index, int count) {
            base.Write(buffer, index, count);
            char[] buffer2 = new char[count]; // Ensures large buffers are not a problem
            for (int i = 0; i < count; i++) buffer2[i] = buffer[index + i];
            WriteGeneric<char[]>(buffer2);
        }

        public override void WriteLine(char[] buffer, int index, int count) {
            base.Write(buffer, index, count);
            char[] buffer2 = new char[count]; // Ensures large buffers are not a problem
            for (int i = 0; i < count; i++) buffer2[i] = buffer[index + i];
            WriteLineGeneric<char[]>(buffer2);
        }
    }

    public class ConsoleRedirectWriter : RedirectWriter {
        TextWriter consoleTextWriter; //keeps Visual Studio console in scope.

        public ConsoleRedirectWriter(TextBox c) {
            consoleTextWriter = Console.Out;
            this.OnWrite += delegate (string text) { consoleTextWriter.Write(text); c.Text += text; c.ScrollToEnd(); };

            Console.SetOut(this);
        }

        public void Release() {
            Console.SetOut(consoleTextWriter);
        }
    }
}
