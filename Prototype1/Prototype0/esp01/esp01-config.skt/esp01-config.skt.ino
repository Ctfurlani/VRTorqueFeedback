#define ESP8266_BAUD 9600

void setup()
{
  Serial.begin(ESP8266_BAUD);
  Serial2.begin(115200);

  delay(1000);
  Serial.flush();
}

void loop()
{
  while(Serial.available())
  {
    Serial2.write(Serial.read());
  }
  while(Serial2.available()) {
    byte b = Serial2.read();
    Serial.write(b);
  }
}
