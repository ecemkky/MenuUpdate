using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;


public class BooksContext : DbContext
{
    public BooksContext()
    {
    }

    public BooksContext(DbContextOptions<BooksContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books { get; set; }
    public DbSet<Publisher> Publishers { get; set; }
    public DbSet<Purchase> Purchases { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=Books;Trusted_Connection=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Author).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.HasOne(d => d.Publisher)
                  .WithMany(p => p.Books)
                  .HasForeignKey(d => d.PublisherId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Books_Publishers");
        });

        modelBuilder.Entity<Publisher>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).IsRequired();
            entity.Property(e => e.UserAddress).IsRequired();
            entity.Property(e => e.CreditCardInfo).IsRequired();
            entity.Property(e => e.PurchaseDate).HasColumnType("datetime");
            entity.HasOne(d => d.Book)
                  .WithMany()
                  .HasForeignKey(d => d.BookId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Purchases_Books");
        });

      
    }

 
}

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public decimal Price { get; set; }

    public int PublisherId { get; set; }
    public Publisher Publisher { get; set; }
}

public class Publisher
{
    public int Id { get; set; }
    public string Name { get; set; }

    public ICollection<Book> Books { get; set; }
}


public class BookRepository
{
    private readonly BooksContext _context;

    public BookRepository(BooksContext context)
    {
        _context = context;
    }

    public void InsertBook(Book book)
    {
        _context.Books.Add(book);
        _context.SaveChanges();
    }

    public void UpdateBook(Book book)
    {
        _context.Books.Update(book);
        _context.SaveChanges();
    }

    public Book GetBookById(int id)
    {
        return _context.Books.Include(b => b.Publisher).FirstOrDefault(b => b.Id == id);
    }

    public List<Book> GetAllBooks()
    {
        return _context.Books.Include(b => b.Publisher).ToList();
    }

    public void BuyBook(Book book, string userName, string userAddress, string creditCardInfo)
    {
        Console.WriteLine("Please enter your name:");
        userName = Console.ReadLine();

        Console.WriteLine("Please enter your address:");
        userAddress = Console.ReadLine();

        Console.WriteLine($"You are buying the book: {book.Title}, Price: {book.Price:C}");

        Console.WriteLine("Please enter your credit card information:");
        creditCardInfo = Console.ReadLine();

        // Store data in database
        var purchase = new Purchase
        {
            Book = book,
            UserName = userName,
            UserAddress = userAddress,
            CreditCardInfo = creditCardInfo,
            PurchaseDate = DateTime.Now
        };

        _context.Purchases.Add(purchase);
        _context.SaveChanges();

        Console.WriteLine("Thank you for your purchase!");
    }
}

public class Purchase
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; }
    public string UserName { get; set; }
    public string UserAddress { get; set; }
    public string CreditCardInfo { get; set; }
    public DateTime PurchaseDate { get; set; }
}

public class PurchaseRepository
{
    private readonly BooksContext _context;

    public PurchaseRepository(BooksContext context)
    {
        _context = context;
    }

    public List<Purchase> GetAllPurchases()
    {
        return _context.Purchases.Include(p => p.Book).ToList();
    }

    public Purchase InsertPurchase(Purchase purchase)
    {
        _context.Purchases.Add(purchase);
        _context.SaveChanges();
        return purchase;
    }
}
class Program
{
    static void Main(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BooksContext>();
        optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Books;Trusted_Connection=True;");

        using (var context = new BooksContext(optionsBuilder.Options))
        {
            var bookRepository = new BookRepository(context);
            var purchaseRepository = new PurchaseRepository(context);

            while (true)
            {
                Console.WriteLine("===============================");
                Console.WriteLine("Welcome to the Book Store");
                Console.WriteLine("===============================");
                Console.WriteLine("Please select an option:");
                Console.WriteLine("1 - Add a book");
                Console.WriteLine("2 - List all books");
                Console.WriteLine("3 - Buy a book");
                Console.WriteLine("4 - List all purchases");
                Console.WriteLine("5 - Exit");
                Console.WriteLine("===============================");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("Please enter the book information:");
                        Console.Write("Title: ");
                        var title = Console.ReadLine();
                        Console.Write("Author: ");
                        var author = Console.ReadLine();
                        Console.Write("Price: ");
                        var price = decimal.Parse(Console.ReadLine());
                        Console.Write("Publisher: ");
                        var publisherName = Console.ReadLine();
                        var book = new Book
                        {
                            Title = title,
                            Author = author,
                            Price = price,
                            Publisher = new Publisher { Name = publisherName }
                        };
                        bookRepository.InsertBook(book);
                        Console.WriteLine("Book added.");
                        break;
                    case "2":
                        var allBooks = bookRepository.GetAllBooks();
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("List of All Books");
                        Console.WriteLine("--------------------------------");
                        foreach (var b in allBooks)
                        {
                            Console.WriteLine($"ID: {b.Id}, Title: {b.Title}, Author: {b.Author}, Price: {b.Price:C}, Publisher: {b.Publisher.Name}");
                        }
                        break;
                    case "3":
                        Console.Write("Please enter the ID of the book you want to buy: ");
                        var bookId = int.Parse(Console.ReadLine());
                        var bookToBuy = bookRepository.GetBookById(bookId);
                        if (bookToBuy == null)
                        {
                            Console.WriteLine("Book not found.");
                            break;
                        }
                        Console.Write("Please enter your name: ");
                        var name = Console.ReadLine();
                        Console.Write("Please enter your address: ");
                        var address = Console.ReadLine();
                        Console.Write("Please enter your credit card number: ");
                        var creditCardNumber = Console.ReadLine();
                        bookRepository.BuyBook(bookToBuy, name, address, creditCardNumber);
                        Console.WriteLine("Book purchased.");

                        var allPurchases = purchaseRepository.GetAllPurchases();
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("List of All Purchases");
                        Console.WriteLine("--------------------------------");
                        foreach (var p in allPurchases)
                        {
                            Console.WriteLine($"ID: {p.Id}, Book Title: {p.Book.Title}, Customer Name: {p.UserName}, Book Id: {p.BookId}");
                        }
                        break;
                    case "4":
                        var allPurchases2 = purchaseRepository.GetAllPurchases();
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("List of All Purchases");
                        Console.WriteLine("---------------------------------");
                        foreach (var p in allPurchases2)
                        {
                            Console.WriteLine($"ID: {p.Id}, Book Title: {p.Book.Title}, Customer Name: {p.UserName}, Book Id: {p.BookId}");
                        }
                        break;
                    case "5":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please select a valid option.");
                        break;
                }
            }
        }
    }


}

