using CampusTrade.API.Services.File;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IO;
using System.Text;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 文件服务单元测试
    /// </summary>
    public class FileServiceTests
    {
        private readonly Mock<ILogger<FileService>> _mockLogger;
        private readonly Mock<IThumbnailService> _mockThumbnailService;
        private readonly FileStorageOptions _options;
        private readonly FileService _fileService;
        private readonly string _testUploadPath;

        public FileServiceTests()
        {
            _mockLogger = new Mock<ILogger<FileService>>();
            _mockThumbnailService = new Mock<IThumbnailService>();
            
            _testUploadPath = Path.Combine(Path.GetTempPath(), "file-service-tests");
            _options = new FileStorageOptions
            {
                UploadPath = _testUploadPath,
                BaseUrl = "http://localhost:5085",
                MaxFileSize = 10 * 1024 * 1024,
                ImageTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                DocumentTypes = new[] { ".pdf", ".txt", ".doc", ".docx" },
                ThumbnailWidth = 200,
                ThumbnailHeight = 200,
                ThumbnailQuality = 80
            };

            var mockOptions = new Mock<IOptions<FileStorageOptions>>();
            mockOptions.Setup(x => x.Value).Returns(_options);

            _fileService = new FileService(_mockLogger.Object, _mockThumbnailService.Object, mockOptions.Object);
        }

        [Fact]
        public void GenerateUniqueFileName_ShouldReturnValidFileName()
        {
            // Arrange
            var originalFileName = "test.jpg";

            // Act
            var result = _fileService.GenerateUniqueFileName(originalFileName);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(originalFileName, result);
            Assert.EndsWith(".jpg", result);
            Assert.Contains("_", result);
        }

        [Fact]
        public void GetThumbnailFileName_ShouldReturnCorrectFormat()
        {
            // Arrange
            var originalFileName = "test.jpg";
            var expected = "test_thumb.jpg";

            // Act
            var result = _fileService.GetThumbnailFileName(originalFileName);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("test.jpg", new[] { ".jpg", ".png" }, true)]
        [InlineData("test.pdf", new[] { ".jpg", ".png" }, false)]
        [InlineData("test.PNG", new[] { ".jpg", ".png" }, true)]
        public void ValidateFileType_ShouldReturnExpectedResult(string fileName, string[] allowedTypes, bool expected)
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);

            // Act
            var result = _fileService.ValidateFileType(mockFile.Object, allowedTypes);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task UploadFileAsync_WithValidImage_ShouldReturnSuccess()
        {
            // Arrange
            var fileName = "test.jpg";
            var fileContent = "test image content";
            var mockFile = CreateMockFile(fileName, fileContent, "image/jpeg");

            _mockThumbnailService.Setup(x => x.GenerateThumbnailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _fileService.UploadFileAsync(mockFile.Object, FileType.ProductImage, true);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileName);
            Assert.NotNull(result.FileUrl);
            Assert.NotNull(result.ThumbnailFileName);
            Assert.NotNull(result.ThumbnailUrl);
            Assert.Equal(fileContent.Length, result.FileSize);
            Assert.Equal("image/jpeg", result.ContentType);

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task UploadFileAsync_WithLargeFile_ShouldReturnFailure()
        {
            // Arrange
            var fileName = "large.jpg";
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile.Setup(f => f.Length).Returns(_options.MaxFileSize + 1);

            // Act
            var result = await _fileService.UploadFileAsync(mockFile.Object, FileType.ProductImage);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("文件大小不能超过", result.ErrorMessage);
        }

        [Fact]
        public async Task UploadFileAsync_WithInvalidFileType_ShouldReturnFailure()
        {
            // Arrange
            var fileName = "test.exe";
            var mockFile = CreateMockFile(fileName, "content", "application/octet-stream");

            // Act
            var result = await _fileService.UploadFileAsync(mockFile.Object, FileType.ProductImage);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("不支持的文件类型", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteFileAsync_WithExistingFile_ShouldReturnTrue()
        {
            // Arrange
            var fileName = "test.jpg";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, "test content");

            // Act
            var result = await _fileService.DeleteFileAsync(fileName);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(filePath));

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task DeleteFileAsync_WithNonExistingFile_ShouldReturnFalse()
        {
            // Arrange
            var fileName = "nonexistent.jpg";

            // Act
            var result = await _fileService.DeleteFileAsync(fileName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task FileExistsAsync_WithExistingFile_ShouldReturnTrue()
        {
            // Arrange
            var fileName = "test.jpg";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, "test content");

            // Act
            var result = await _fileService.FileExistsAsync(fileName);

            // Assert
            Assert.True(result);

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task FileExistsAsync_WithNonExistingFile_ShouldReturnFalse()
        {
            // Arrange
            var fileName = "nonexistent.jpg";

            // Act
            var result = await _fileService.FileExistsAsync(fileName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DownloadFileAsync_WithExistingFile_ShouldReturnSuccess()
        {
            // Arrange
            var fileName = "test.jpg";
            var content = "test content";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, content);

            // Act
            var result = await _fileService.DownloadFileAsync(fileName);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileStream);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal("image/jpeg", result.ContentType);

            // Cleanup
            result.FileStream?.Dispose();
            CleanupTestFiles();
        }

        [Fact]
        public async Task DownloadFileAsync_WithNonExistingFile_ShouldReturnFailure()
        {
            // Arrange
            var fileName = "nonexistent.jpg";

            // Act
            var result = await _fileService.DownloadFileAsync(fileName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("文件不存在", result.ErrorMessage);
        }

        private Mock<IFormFile> CreateMockFile(string fileName, string content, string contentType)
        {
            var mockFile = new Mock<IFormFile>();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Returns((Stream target, CancellationToken token) => 
                {
                    stream.Position = 0;
                    return stream.CopyToAsync(target, token);
                });

            return mockFile;
        }

        private void CleanupTestFiles()
        {
            if (Directory.Exists(_testUploadPath))
            {
                Directory.Delete(_testUploadPath, true);
            }
        }
    }
}
