namespace RoMi.Tests;

[TestFixture]
public class StartAddressTests
{
    [Test]
    [TestCase(new byte[] { 0x01, 0x02, 0x03, 0x04 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x01 })]
    public void Constructor_WithValidBytes_ShouldSetBytesCorrectly(byte[] bytes)
    {
        // Act
        StartAddress startAddress = new(bytes);

        // Assert
        Assert.That(startAddress.Bytes, Is.EqualTo(bytes));
    }

    [Test]
    [TestCase("01 02 03 04", new byte[] { 0x01, 0x02, 0x03, 0x04 })]
    [TestCase(   "01 02 03", new byte[] { 0x00, 0x01, 0x02, 0x03 })]
    [TestCase(      "01 02", new byte[] { 0x00, 0x00, 0x01, 0x02 })]
    public void Constructor_WithValidHexString_ShouldConvertHexStringToBytes(string hexString, byte[] expectedBytes)
    {
        // Act
        StartAddress startAddress = new(hexString);

        // Assert
        Assert.That(startAddress.Bytes, Is.EqualTo(expectedBytes));
    }

    [Test]
    [TestCase("00 00 00 80")]
    [TestCase("00 00 80 00")]
    [TestCase("00 80 00 00")]
    [TestCase("80 00 00 00")]
    public void Constructor_WithTooBigByteValues_ShouldThrowArgumentException(string hexString)
    {
        // Act
        var ex = Assert.Throws<ArgumentException>(() => new StartAddress(hexString));

        // Assert
        Assert.That(ex!.Message, Does.StartWith("Each single byte of the array must be in the range between 0 (0x00) and 127 (0x7F)"));
    }

    [Test]
    [TestCase("01 02 03 04 05")]
    public void Constructor_WithTooLongHexString_ShouldThrowArgumentException(string hexString)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new StartAddress(hexString));
    }

    [Test]
    [TestCase("01")]
    [TestCase("")]
    public void Constructor_WithTooShortHexString_ShouldThrowArgumentException(string hexString)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new StartAddress(hexString));
    }

    [Test]
    [TestCase("01 02 03 4")]
    [TestCase("001 2 3 040")]
    [TestCase("01020304")]
    public void Constructor_WithMalformedHexString_ShouldThrowArgumentException(string hexString)
    {
        // Act
        var ex = Assert.Throws<ArgumentException>(() => new StartAddress(hexString));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Malformed hex string. (Parameter 'hexString')"));
    }

    [Test]
    [TestCase("0x00 0x01")]
    [TestCase("01 0G")] // only chars A-F allowed
    [TestCase("01 0a")] // only capital chars allowed
    public void Constructor_WithInvalidHexString_ShouldThrowArgumentException(string hexString)
    {
        // Act
        var ex = Assert.Throws<ArgumentException>(() => new StartAddress(hexString));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Malformed hex string. (Parameter 'hexString')"));
    }

    [Test]
    public void BytesCopy_ShouldReturnDeepCopyOfBytes()
    {
        // Arrange
        byte[] bytes = [0x01, 0x02, 0x03, 0x04];
        StartAddress startAddress = new(bytes);

        // Act
        byte[] copy = startAddress.BytesCopy();

        // Assert
        Assert.That(copy, Is.EqualTo(bytes));
        Assert.That(copy, Is.Not.SameAs(bytes)); // Ensure it is a deep copy
    }

    [Test]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0)]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x01 }, 1)]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x7F }, 127)]
    [TestCase(new byte[] { 0x00, 0x00, 0x01, 0x00 }, 128)]
    [TestCase(new byte[] { 0x00, 0x00, 0x7F, 0x00 }, 16256)]
    [TestCase(new byte[] { 0x00, 0x01, 0x00, 0x00 }, 16384)]
    [TestCase(new byte[] { 0x00, 0x7F, 0x00, 0x0 }, 2080768)]
    [TestCase(new byte[] { 0x01, 0x00, 0x00, 0x0 }, 2097152)]
    [TestCase(new byte[] { 0x7F, 0x00, 0x00, 0x0 }, 266338304)]
    public void ToIntegerRepresentation_ShouldConvertBytesToInteger(byte[] bytes, int expectedInteger)
    {
        // Arrange
        StartAddress startAddress = new(bytes);

        // Act
        int result = startAddress.ToIntegerRepresentation();

        // Assert
        Assert.That(result, Is.EqualTo(expectedInteger));
    }

    [Test]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x80 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x80, 0x00 })]
    [TestCase(new byte[] { 0x00, 0x80, 0x00, 0x00 })]
    [TestCase(new byte[] { 0x80, 0x00, 0x00, 0x00 })]
    public void ToIntegerRepresentation_WithTooBigByteValues_ShouldThrowArgumentException(byte[] bytes)
    {
        // Arrange
        StartAddress startAddress = new(bytes);

        // Act
        var ex = Assert.Throws<ArgumentException>(() => startAddress.ToIntegerRepresentation());

        // Assert
        Assert.That(ex!.Message, Does.StartWith("Each single byte of the array must be in the range between 0 (0x00) and 127 (0x7F)"));
    }

    [Test]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x00, 0x00, 0x01 }, new byte[] { 0x00, 0x00, 0x00, 0x01 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x00, 0x00, 0x7F }, new byte[] { 0x00, 0x00, 0x00, 0x7F })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x00, 0x01, 0x00 }, new byte[] { 0x00, 0x00, 0x01, 0x00 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x00, 0x7F, 0x00 }, new byte[] { 0x00, 0x00, 0x7F, 0x00 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x00, 0x00 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x7F, 0x00, 0x00 }, new byte[] { 0x00, 0x7F, 0x00, 0x00 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x01, 0x00, 0x00, 0x00 }, new byte[] { 0x01, 0x00, 0x00, 0x00 })]
    [TestCase(new byte[] { 0x7E, 0x00, 0x00, 0x00 }, new byte[] { 0x7F, 0x00, 0x00, 0x00 }, new byte[] { 0x01, 0x00, 0x00, 0x00 })]
    [TestCase(new byte[] { 0x7E, 0x7E, 0x7E, 0x7E }, new byte[] { 0x7F, 0x7F, 0x7F, 0x7F }, new byte[] { 0x01, 0x01, 0x01, 0x01 })]
    public void CalculateOffset_ShouldReturnCorrectOffset(byte[] lower, byte[] higher, byte[] expectedOffset)
    {
        // Arrange
        StartAddress lowerStartAddress = new(lower);
        StartAddress higherStartAddress = new(higher);;

        // Act
        byte[] result = StartAddress.CalculateOffset(lowerStartAddress, higherStartAddress);

        // Assert
        Assert.That(result, Is.EqualTo(expectedOffset));
    }

    [Test]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x01 }, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
    public void CalculateOffset_ShouldThrowException(byte[] lower, byte[] higher)
    {
        // Arrange
        StartAddress lowerStartAddress = new(lower);
        StartAddress higherStartAddress = new(higher); ;

        // Act
        var ex = Assert.Throws<ArgumentException>(() => StartAddress.CalculateOffset(lowerStartAddress, higherStartAddress));

        // Assert
        Assert.That(ex!.Message, Does.StartWith("The first argument must contain a lower value than the second."));
    }

    [Test]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x01 }, new byte[] { 0x00, 0x00, 0x00, 0x01 }, new byte[] { 0x00, 0x00, 0x00, 0x02 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x7F }, new byte[] { 0x00, 0x00, 0x00, 0x01 }, new byte[] { 0x00, 0x00, 0x01, 0x00 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x01 }, new byte[] { 0x00, 0x00, 0x00, 0x7F }, new byte[] { 0x00, 0x00, 0x01, 0x00 })]
    [TestCase(new byte[] { 0x00, 0x7F, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x00, 0x00 }, new byte[] { 0x01, 0x00, 0x00, 0x00 })]
    [TestCase(new byte[] { 0x7E, 0x00, 0x00, 0x00 }, new byte[] { 0x01, 0x00, 0x00, 0x00 }, new byte[] { 0x7F, 0x00, 0x00, 0x00 })]
    public void Increment_ShouldAddValueToBytes(byte[] original, byte[] valueToAdd, byte[] expected)
    {
        // Arrange
        StartAddress startAddress = new(original);

        // Act
        startAddress.Increment(valueToAdd);

        // Assert
        Assert.That(startAddress.Bytes, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(new byte[] { 0x7F, 0x00, 0x00, 0x00 }, new byte[] { 0x01, 0x7F, 0x00, 0x00 })]
    [TestCase(new byte[] { 0x01, 0x00, 0x00, 0x00 }, new byte[] { 0x7F, 0x7F, 0x00, 0x00 })]
    public void Increment_ShouldThrowOverflowException(byte[] original, byte[] valueToAdd)
    {
        // Arrange
        StartAddress startAddress = new(original);        

        // Act
        var ex = Assert.Throws<OverflowException>(() => startAddress.Increment(valueToAdd));

        // Assert
        Assert.That(ex!.Message, Does.StartWith("First byte of StartAddress is bigger than 127: 128"));
    }

    [Test]
    public void Equals_WithIdenticalStartAddresses_ShouldReturnTrue()
    {
        // Arrange
        StartAddress address1 = new([0x01, 0x02, 0x03, 0x04]);
        StartAddress address2 = new([0x01, 0x02, 0x03, 0x04]);

        // Act & Assert
        Assert.That(address1.Equals(address2), Is.True);
    }

    [Test]
    public void Equals_WithDifferentStartAddresses_ShouldReturnFalse()
    {
        // Arrange
        StartAddress address1 = new([0x01, 0x02, 0x03, 0x04]);
        StartAddress address2 = new([0x05, 0x06, 0x07, 0x08]);

        // Act & Assert
        Assert.That(address1.Equals(address2), Is.False);
    }
}
