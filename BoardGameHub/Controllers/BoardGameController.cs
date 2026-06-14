using BoardGameHub.Models;
using BoardGameHub.Models.DbModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;

namespace BoardGameHub.Controllers
{
    public class BoardGameController : Controller
    {
        private readonly DataBaseContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BoardGameController(DataBaseContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. Wyświetlanie katalogu gier
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult ViewAll()
        {
            var boardGames = _context.BoardGames
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .ToList();
            return View(boardGames);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult UserViewAll(int? categoryId, string? status, int? playerCount)
        {
            var boardGames = _context.BoardGames
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .AsEnumerable()
                .ToList();

            // Filtrowanie po kategorii
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                boardGames = boardGames.Where(b => b.CategoryId == categoryId.Value).ToList();
            }

            // Filtrowanie po dostępności
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<GameStatus>(status, out var statusEnum))
                {
                    boardGames = boardGames.Where(b => b.Status == statusEnum).ToList();
                }
            }

            // Filtrowanie po liczbie graczy
            if (playerCount.HasValue && playerCount.Value > 0)
            {
                boardGames = boardGames.Where(b => b.MinPlayers <= playerCount.Value && playerCount.Value <= b.MaxPlayers).ToList();
            }

            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name", categoryId);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(GameStatus)).Cast<GameStatus>(), status);
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedPlayerCount = playerCount;

            return View(boardGames);
        }

        // Szczegóły gry dla użytkowników
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Details(int id)
        {
            var boardGame = _context.BoardGames
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .FirstOrDefault(b => b.BoardGameId == id);

            if (boardGame == null) return NotFound();
            return View(boardGame);
        }

        // 2. Dodawanie gry (GET)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new BoardGame());
        }

        // 2. Dodawanie gry (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BoardGame boardGame, IFormFile? imageFile)
        {
            ModelState.Remove(nameof(boardGame.Category));
            ModelState.Remove(nameof(boardGame.Publisher));

            if (boardGame.MinPlayers < 1)
            {
                ModelState.AddModelError(nameof(boardGame.MinPlayers), "Minimalna liczba graczy musi być większa niż 0");
            }
            if (boardGame.MaxPlayers < boardGame.MinPlayers)
            {
                ModelState.AddModelError(nameof(boardGame.MaxPlayers), "Maksymalna liczba graczy musi być większa lub równa minimalnej");
            }

            if (!ModelState.IsValid)
            {
                PopulateDropdowns();
                return View(boardGame);
            }

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    boardGame.ImagePath = SaveImage(imageFile);
                }

                _context.BoardGames.Add(boardGame);
                _context.SaveChanges();

                return RedirectToAction(nameof(ViewAll));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Wystąpił błąd podczas zapisu.");
                PopulateDropdowns();
                return View(boardGame);
            }
        }

        // 3. Edycja gry (GET)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var boardGame = _context.BoardGames.FirstOrDefault(x => x.BoardGameId == id);
            if (boardGame == null) return NotFound();

            PopulateDropdowns(boardGame.CategoryId, boardGame.PublisherId);
            return View(boardGame);
        }

        // 3. Edycja gry (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(BoardGame boardGame, IFormFile? imageFile, string? currentImagePath)
        {
            ModelState.Remove(nameof(boardGame.Category));
            ModelState.Remove(nameof(boardGame.Publisher));

            if (boardGame.MinPlayers < 1)
            {
                ModelState.AddModelError(nameof(boardGame.MinPlayers), "Minimalna liczba graczy musi być większa niż 0");
            }
            if (boardGame.MaxPlayers < boardGame.MinPlayers)
            {
                ModelState.AddModelError(nameof(boardGame.MaxPlayers), "Maksymalna liczba graczy musi być większa lub równa minimalnej");
            }

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(boardGame.CategoryId, boardGame.PublisherId);
                return View(boardGame);
            }

            try
            {
                var existing = _context.BoardGames
                    .AsNoTracking()
                    .FirstOrDefault(b => b.BoardGameId == boardGame.BoardGameId);

                if (imageFile != null && imageFile.Length > 0)
                {
                    var newPath = SaveImage(imageFile);

                    if (!string.IsNullOrEmpty(existing?.ImagePath))
                    {
                        try
                        {
                            var oldRelative = existing.ImagePath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
                            var oldFull = Path.Combine(_webHostEnvironment.WebRootPath ?? "wwwroot", oldRelative);
                            if (System.IO.File.Exists(oldFull))
                            {
                                System.IO.File.Delete(oldFull);
                            }
                        }
                        catch
                        {
                        }
                    }

                    boardGame.ImagePath = newPath;
                }
                else
                {
                    boardGame.ImagePath = !string.IsNullOrEmpty(currentImagePath)
                        ? currentImagePath
                        : existing?.ImagePath;
                }

                _context.BoardGames.Update(boardGame);
                _context.SaveChanges();
                return RedirectToAction(nameof(ViewAll));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Błąd przy zapisie. Sprawdź logi.");
                PopulateDropdowns(boardGame.CategoryId, boardGame.PublisherId);
                return View(boardGame);
            }
        }

        // 4. Usuwanie gry (GET)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var boardGame = _context.BoardGames
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .FirstOrDefault(x => x.BoardGameId == id);

            if (boardGame == null) return NotFound();

            return View(boardGame);
        }

        // 4. Usuwanie gry (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirm(int id)
        {
            var boardGame = _context.BoardGames.FirstOrDefault(x => x.BoardGameId == id);
            if (boardGame != null)
            {
                _context.BoardGames.Remove(boardGame);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(ViewAll));
        }


        private void PopulateDropdowns(object? selectedCategory = null, object? selectedPublisher = null)
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "Name", selectedCategory);
            ViewBag.PublisherId = new SelectList(_context.Publishers, "PublisherId", "Name", selectedPublisher);
        }

        private string SaveImage(IFormFile imageFile)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath ?? "wwwroot", "images");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var originalFileName = Path.GetFileName(imageFile.FileName);
            var safeFileName = $"{Guid.NewGuid():N}_{originalFileName}";
            var filePath = Path.Combine(uploadsFolder, safeFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(fileStream);
            }

            return "/images/" + safeFileName;
        }
    }
}