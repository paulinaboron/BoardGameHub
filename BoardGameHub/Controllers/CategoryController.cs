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

        // Wstrzykiwanie zależności do kontekstu bazy danych
        public CategoryController(DataBaseContext context)
        {
            _context = context;
        }

        // 1. READ - Wyświetlanie listy wszystkich kategorii
        [HttpGet]
        public IActionResult ViewAll()
        {
            var categories = _context.Categories.ToList(); // Pobranie wszystkich kategorii z bazy
            return View(categories);
        }

        // 2. CREATE - Dodawanie nowej kategorii (GET: Zwraca pusty formularz)
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Category()); // W .NET 8 używamy typu IActionResult zamiast ActionResult
        }

        // 2. CREATE - Dodawanie nowej kategorii (POST: Odbiera dane i zapisuje w bazie)
        [HttpPost]
        [ValidateAntiForgeryToken] // Zabezpieczenie przed atakami CSRF
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

        // 3. UPDATE - Edycja istniejącej kategorii (GET: Pobiera dane do formularza)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var category = _context.Categories.FirstOrDefault(x => x.CategoryId == id);
            if (category == null) return NotFound();

            return View(category);
        }

        // 3. UPDATE - Edycja istniejącej kategorii (POST: Zapisuje wprowadzone zmiany)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (!ModelState.IsValid) return View(category);

            _context.Categories.Update(category); // EF Core automatycznie śledzi i aktualizuje zmiany
            _context.SaveChanges();

            return RedirectToAction(nameof(ViewAll));
        }

        // 4. DELETE - Usuwanie kategorii (GET: Ekran pytający o potwierdzenie)
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = _context.Categories.FirstOrDefault(x => x.CategoryId == id);
            if (category == null) return NotFound();

            return View(category);
        }

        // 4. DELETE - Usuwanie kategorii (POST: Faktyczne skasowanie rekordu po potwierdzeniu)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirm(int id)
        {
            var category = _context.Categories.FirstOrDefault(x => x.CategoryId == id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(ViewAll));
        }
    }
}
