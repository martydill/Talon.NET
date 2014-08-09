using NUnit.Framework;
using Shouldly;

namespace Talon.Tests
{
    class UtilsTest
    {
        [Test]
        public void splitlines_splits_on_all_line_types()
        {
            var msg = "hello\rworld\ntest\r\nabcd";
            var results = msg.SplitLines();
            results.ShouldBe(new[] {"hello", "world", "test", "abcd"});
        }

        [Test]
        public void splitlines_removes_trailing_newline()
        {
            var msg = "hello\n\nworld\n\n";
            var results = msg.SplitLines();
            results.ShouldBe(new[] {"hello", "", "world", ""});
        }
    }
}
