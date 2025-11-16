CREATE TABLE IF NOT EXISTS Payments (
    Id UUID PRIMARY KEY,
    Amount DECIMAL(18, 2) NOT NULL,
    Method VARCHAR(50) NOT NULL,
    Status VARCHAR(50) NOT NULL,
    CardToken VARCHAR(255),
    Installments INT,
    CreatedAt TIMESTAMPTZ NOT NULL,
    UpdatedAt TIMESTAMPTZ
    );

CREATE INDEX IF NOT EXISTS idx_payments_status ON Payments(Status);