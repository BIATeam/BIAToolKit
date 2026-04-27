using Xunit;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;

namespace BIA.ToolKit.Tests.Unit;

/// <summary>
/// Sample tests to verify xUnit and FsCheck are working correctly
/// </summary>
public class SampleTest
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        var expected = 42;
        
        // Act
        var actual = 40 + 2;
        
        // Assert
        actual.Should().Be(expected);
    }
    
    [Property(MaxTest = 100)]
    public Property AdditionIsCommutative_PropertyTest()
    {
        // Property: For all integers a and b, a + b = b + a
        return Prop.ForAll<int, int>((a, b) =>
        {
            var leftToRight = a + b;
            var rightToLeft = b + a;
            
            return leftToRight == rightToLeft;
        });
    }
    
    [Property(MaxTest = 100)]
    public Property StringConcatenationLength_PropertyTest()
    {
        // Property: For all strings s1 and s2, (s1 + s2).Length = s1.Length + s2.Length
        return Prop.ForAll<string, string>((s1, s2) =>
        {
            // Handle null strings
            s1 ??= string.Empty;
            s2 ??= string.Empty;
            
            var concatenated = s1 + s2;
            
            return concatenated.Length == s1.Length + s2.Length;
        });
    }
}
