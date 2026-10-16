using System.ComponentModel;

namespace bai2_16_10
{
    // Lớp sản phẩm trong kho. ImagePath chỉ lưu ĐƯỜNG DẪN ảnh, không lưu ảnh vào đối tượng.
    public class Product : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _category;
        private decimal _price;
        private int _quantity;
        private string _imagePath;

        public string Id { get => _id; set { _id = value; Notify(nameof(Id)); } }
        public string Name { get => _name; set { _name = value; Notify(nameof(Name)); } }
        public string Category { get => _category; set { _category = value; Notify(nameof(Category)); } }
        public decimal Price { get => _price; set { _price = value; Notify(nameof(Price)); } }
        public int Quantity { get => _quantity; set { _quantity = value; Notify(nameof(Quantity)); } }
        public string ImagePath { get => _imagePath; set { _imagePath = value; Notify(nameof(ImagePath)); } }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}