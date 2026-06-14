using BoardGameHub.Models;
using BoardGameHub.Models.DbModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameHub.Controllers
{
    [Authorize(Roles = "Admin")]

    public class CategoryController : Controller
    {
        private readonly DataBaseContext _context;

        public CategoryController(DataBaseContext context)
        {
            _context = context;
        }

        // 1. Wyświetlanie listy wszystkich kategorii
        [HttpGet]
        public IActionResult ViewAll()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        // 2. Dodawanie nowej kategorii (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Category());
        }

        // 2.  Dodawanie nowej kategorii (POST)
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                _context.SaveChanges();
                return RedirectToAction(nameof(ViewAll));
            }
            return View(category);
        }

        // 3. Edycja istniejącej kategorii (GET)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var category = _context.Categories.FirstOrDefault(x => x.CategoryId == id);
            if (category == null) return NotFound();

            return View(category);
        }

        // 3. Edycja istniejącej kategorii (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (!ModelState.IsValid) return View(category);

            _context.Categories.Update(category);
            _context.SaveChanges();

            return RedirectToAction(nameof(ViewAll));
        }

        // 4. Usuwanie kategorii (GET)
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = _context.Categories.FirstOrDefault(x => x.CategoryId == id);
            if (category == null) return NotFound();

            var gamesWithCategory = _context.BoardGames.Where(b => b.CategoryId == id).Count();
            if (gamesWithCategory > 0)
            {
                TempData["Error"] = $"Nie można usunąć tej kategorii. Jest używana w {gamesWithCategory} grze(ach).";
                return RedirectToAction(nameof(ViewAll));
            }

            return View(category);
        }

        // 4. Usuwanie kategorii (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirm(int id)
        {
            var category = _context.Categories.FirstOrDefault(x => x.CategoryId == id);
            if (category != null)
            {
                var gamesWithCategory = _context.BoardGames.Where(b => b.CategoryId == id).Count();
                if (gamesWithCategory > 0)
                {
                    TempData["Error"] = $"Nie można usunąć tej kategorii. Jest używana w {gamesWithCategory} grze(ach).";
                    return RedirectToAction(nameof(ViewAll));
                }

                _context.Categories.Remove(category);
                _context.SaveChanges();
                TempData["Success"] = "Kategoria została pomyślnie usunięta.";
            }
            return RedirectToAction(nameof(ViewAll));
        }
    }
}
