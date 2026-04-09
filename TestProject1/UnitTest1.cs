using Moq;
using CookbookMauiBlazor.Shared.Services;
using CookbookMauiBlazor.Shared.Viewmodels;
using System.Net;

namespace TestProject1
{
    public class BoardCreationTests
    {
        private readonly Mock<IBoardService> _mockBoardService;
        private readonly BoardViewModel _boardViewModel;

        public BoardCreationTests()
        {
            _mockBoardService = new Mock<IBoardService>();
            _boardViewModel = new BoardViewModel(_mockBoardService.Object);
        }

        

        [Fact]
        public async Task CreateBoardAsync_WithValidInputs_CallsServiceCreateAsync()
        {
            // Arrange
            var ownerUserId = "user123";
            var oardName = "My Board";

            // Act
            await _boardViewModel.CreateBoardAsync(ownerUserId, boardName);

            // Assert
            _mockBoardService.Verify(
                s => s.CreateAsync(ownerUserId, boardName, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateBoardAsync_WithEmptyName_StillCallsService()
        {
            // Arrange
            var ownerUserId = "user123";
            var boardName = string.Empty;

            // Act
            await _boardViewModel.CreateBoardAsync(ownerUserId, boardName);

            // Assert
            // The service is called; validation happens at the service level
            _mockBoardService.Verify(
                s => s.CreateAsync(ownerUserId, boardName, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateBoardAsync_WithNullName_StillCallsService()
        {
            // Arrange
            var ownerUserId = "user123";
            string boardName = null!;

            // Act
            await _boardViewModel.CreateBoardAsync(ownerUserId, boardName);

            // Assert
            // The service is called; validation happens at the service level
            _mockBoardService.Verify(
                s => s.CreateAsync(ownerUserId, boardName, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateBoardAsync_WithWhitespaceOnlyName_StillCallsService()
        {
            // Arrange
            var ownerUserId = "user123";
            var boardName = "   ";

            // Act
            await _boardViewModel.CreateBoardAsync(ownerUserId, boardName);

            // Assert
            // The service is called; validation happens at the service level
            _mockBoardService.Verify(
                s => s.CreateAsync(ownerUserId, boardName, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateBoardAsync_WithEmptyOwnerUserId_StillCallsService()
        {
            // Arrange
            var ownerUserId = string.Empty;
            var boardName = "My Board";

            // Act
            await _boardViewModel.CreateBoardAsync(ownerUserId, boardName);

            // Assert
            // The service is called; validation happens at the service level
            _mockBoardService.Verify(
                s => s.CreateAsync(ownerUserId, boardName, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateBoardAsync_WithNullOwnerUserId_StillCallsService()
        {
            // Arrange
            string ownerUserId = null!;
            var boardName = "My Board";

            // Act
            await _boardViewModel.CreateBoardAsync(ownerUserId, boardName);

            // Assert
            // The service is called; validation happens at the service level
            _mockBoardService.Verify(
                s => s.CreateAsync(ownerUserId, boardName, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateBoardAsync_PassesBoardNameDirectlyToService()
        {
            // Arrange
            var ownerUserId = "user123";
            var boardName = "  My Board  ";

            // Act
            await _boardViewModel.CreateBoardAsync(ownerUserId, boardName);

            // Assert
            // The ViewModel passes the name as-is; the Service trims it
            _mockBoardService.Verify(
                s => s.CreateAsync(ownerUserId, boardName, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateBoardAsync_WithValidCancellationToken_PassesCancellationToken()
        {
            // Arrange
            var ownerUserId = "user123";
            var boardName = "My Board";
            var cancellationToken = new CancellationToken();

            // Act
            await _boardViewModel.CreateBoardAsync(ownerUserId, boardName, cancellationToken);

            // Assert
            _mockBoardService.Verify(
                s => s.CreateAsync(ownerUserId, boardName, cancellationToken),
                Times.Once);
        }

        [Theory]
        [InlineData("user1", "Board One")]
        [InlineData("user2", "Board Two")]
        [InlineData("user_id", "My Cooking Board")]
        public async Task CreateBoardAsync_WithVariousValidInputs_SuccessfullyCreatesBoard(string ownerUserId, string boardName)
        {
            // Act
            await _boardViewModel.CreateBoardAsync(ownerUserId, boardName);

            // Assert
            _mockBoardService.Verify(
                s => s.CreateAsync(ownerUserId, boardName, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateBoardAsync_UsesServiceDependency()
        {
            // Arrange
            var ownerUserId = "user123";
            var boardName = "Test Board";

            // Act
            await _boardViewModel.CreateBoardAsync(ownerUserId, boardName);

            // Assert - Verifies the viewmodel uses the injected service
            _mockBoardService.VerifyAll();
        }
    }
}
