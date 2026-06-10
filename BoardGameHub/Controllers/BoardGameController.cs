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
        private readonly IWebHostEnvironment _webHostEnvironment; // Potrzebne do zapisu zdjęć w wwwroot 

        public BoardGameController(DataBaseContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. Wyświetlanie katalogu gier (Z użyciem .Include, aby pobrać też nazwy Kategorii i Wydawców)
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
        public IActionResult UserViewAll()
        {
            var boardGames = _context.BoardGames
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .ToList();
            return View(boardGames);
        }

        // Szczegóły gry — dostęp dla użytkowników
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

        // 2. Dodawanie gry (GET) - przygotowujemy listy rozwijane dla Kategorii i Wydawców
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new BoardGame());
        }

        // 2. Dodawanie gry (POST) - odbiera dane gry oraz przesłany plik graficzny (IFormFile) 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BoardGame boardGame, IFormFile? imageFile)
        {
            // Usuń walidację właściwości nawigacyjnych (jeśli nie zmieniłeś modelu na nullable)
            ModelState.Remove(nameof(boardGame.Category));
            ModelState.Remove(nameof(boardGame.Publisher));

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
                // Zaloguj wyjątek jeśli masz logger; dla użytkownika pokaż powiadomienie
                ModelState.AddModelError(string.Empty, "Wystąpił błąd podczas zapisu. Sprawdź logi.");
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
            // Usuń walidację właściwości nawigacyjnych, które nie są przesyłane przez formularz
            ModelState.Remove(nameof(boardGame.Category));
            ModelState.Remove(nameof(boardGame.Publisher));

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(boardGame.CategoryId, boardGame.PublisherId);
                return View(boardGame);
            }

            try
            {
                // Pobierz istniejący rekord z DB, żeby mieć oryginalną ImagePath jeśli trzeba
                var existing = _context.BoardGames
                    .AsNoTracking()
                    .FirstOrDefault(b => b.BoardGameId == boardGame.BoardGameId);

                if (imageFile != null && imageFile.Length > 0)
                {
                    // zapisujemy nowe zdjęcie i (opcjonalnie) usuwamy poprzednie z dysku
                    var newPath = SaveImage(imageFile);

                    // spróbuj usunąć stary plik (bez awarii jeśli nie istnieje)
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
                            // ignoruj błąd usuwania pliku
                        }
                    }

                    boardGame.ImagePath = newPath;
                }
                else
                {
                    // jeśli currentImagePath przekazany, użyj go; w przeciwnym razie użyj istniejącego z DB
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

        // --- POMOCNICZE METODY ---

        // Metoda ładująca dane do SelectList, które stworzą dropdowny (menu rozwijane) w widoku HTML
        private void PopulateDropdowns(object? selectedCategory = null, object? selectedPublisher = null)
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "Name", selectedCategory);
            ViewBag.PublisherId = new SelectList(_context.Publishers, "PublisherId", "Name", selectedPublisher);
        }

        // Ulepszona metoda zapisu pliku
        private string SaveImage(IFormFile imageFile)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath ?? "wwwroot", "images");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Użyj nazwy pliku bez ścieżek i usuń ewentualne niebezpieczne znaki
            var originalFileName = Path.GetFileName(imageFile.FileName);
            var safeFileName = $"{Guid.NewGuid():N}_{originalFileName}";
            var filePath = Path.Combine(uploadsFolder, safeFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(fileStream);
            }

            // Zwracaj ścieżkę względną do wwwroot (możesz też zapisywać "~/" i potem użyć Url.Content)
            return "/images/" + safeFileName;
        }
    }
}