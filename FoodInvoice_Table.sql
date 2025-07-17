-- Create FoodInvoice table
CREATE TABLE FoodInvoice (
    FoodInvoice_ID INT IDENTITY(1,1) PRIMARY KEY,
    Invoice_ID NVARCHAR(10) NOT NULL,
    Food_ID INT NOT NULL,
    Quantity INT NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_FoodInvoice_Invoice FOREIGN KEY (Invoice_ID) REFERENCES Invoice(Invoice_ID),
    CONSTRAINT FK_FoodInvoice_Food FOREIGN KEY (Food_ID) REFERENCES Food(FoodId)
);

-- Add indexes for better performance
CREATE INDEX IX_FoodInvoice_InvoiceID ON FoodInvoice(Invoice_ID);
CREATE INDEX IX_FoodInvoice_FoodID ON FoodInvoice(Food_ID); 