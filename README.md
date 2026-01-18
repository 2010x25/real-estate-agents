# 🏡 Real Estate AI Agent System .NET, Azure Open AI, and Foundry

## 🧭 Strategic Thinking & System Design Philosophy

This project is designed not just as a technical implementation, but as a **reference architecture** for building **AI-powered, data-driven systems**. As a technical consultant, the guiding principle is:

> **High-quality data + well-designed retrieval + clear agent responsibilities = reliable AI outcomes**

Before discussing code, it is important to understand *how to think* about the system from a strategic and architectural perspective.

---

### 1️⃣ Thinking About Data Ingestion (The Foundation)

AI systems are only as good as the data they rely on. In this project, data ingestion is treated as a **first-class architectural concern**, not an afterthought.

Key considerations:

#### ✅ Data Quality
- Extract **human-readable, semantically meaningful text** (titles, descriptions, addresses).
- Avoid UI noise such as ads, banners, and duplicated DOM elements.
- Normalize text (trim, remove excessive whitespace, handle missing fields).

#### 📦 Data Volume
- Each property page represents a **single knowledge unit**.
- Pages are ingested independently to avoid cross-contamination of context.
- This design supports horizontal scaling as the number of URLs grows.

#### 🧹 Data Cleaning & Structure
- Convert raw HTML into a **strongly typed domain model** (`PropertyDetail`).
- Enforce consistent fields across all records, even when values are missing.
- Structured JSON becomes a stable contract between ingestion and AI layers.

This deliberate ingestion strategy ensures that embeddings are created from **clean, trustworthy, and relevant content**.

---

### 2️⃣ Why Separate Ingestion From AI Reasoning?

The system is intentionally split into **two independent projects**:

- **Project 1** → Data ingestion & normalization
- **Project 2** → AI reasoning & orchestration

This separation allows:
- Independent evolution of scraping logic
- Re-embedding data without re-scraping
- Reuse of the same dataset across multiple AI experiments

---

This repository demonstrates an **end-to-end AI-powered real estate information system** built with **C#**, **Puppeteer Sharp**, and **Microsoft Agent Framework**. The solution is split into **two projects**:

1. **Web Data Ingestion & Scraping** – Collects and structures property data from real estate URLs.
2. **AI Agent Orchestration & RAG** – Uses multiple AI agents with vector embeddings to answer user queries and perform translations.

---

## ✨ High-Level Architecture
  <img src="https://github.com/2010x25/real-estate-agents/blob/main/overview.PNG"/>
  
```
Text File (URLs)
      │
      ▼
[ Project 1 ] Web Scraper (Puppeteer Sharp)
      │
      ▼
Structured Property JSON
      │
      ▼
[ Project 2 ] Embedding Utility
      │
      ▼
In-Memory Vector Store (text-embedding-small)
      │
      ▼
AI Agent Orchestrator
   ├── RAG Agent (Vector Search)
   └── Translation Agent (EN → ES)
```

---

### 3️⃣ Choosing Azure AI & GPT-4o (Platform Strategy)

From a consulting and enterprise architecture standpoint, the AI platform choice is critical. This project intentionally uses **Azure AI (Azure OpenAI / Azure AI Foundry)** and **GPT-4o** to balance **capability, governance, and scalability**.

#### ☁️ Why Azure AI?

Azure AI is designed for **enterprise-grade AI systems**, making it a strong fit for production-ready architectures.

Key reasons:

- **Enterprise Security & Compliance**
  - Azure AD–based authentication
  - Private networking, RBAC, and managed identities
  - Compliance with common enterprise standards (SOC, ISO, GDPR-ready)

- **Operational Control**
  - Clear separation of resources (models, deployments, environments)
  - Predictable quotas and rate limits
  - Centralized monitoring and logging

- **Future-Proofing**
  - Ability to swap models or providers without rewriting business logic
  - Native support for embeddings, chat, tools, and agents

In short, Azure AI enables moving from *prototype to production* without re-architecting the system.

---

#### 🧠 Why GPT-4o?

GPT-4o is selected not just for raw intelligence, but for **balanced multimodal reasoning and efficiency**.

Strategic advantages:

- **Strong Reasoning for Orchestration**
  - Ideal for the orchestrator agent to understand intent, route tasks, and manage handoffs

- **High-Quality Natural Language Output**
  - Produces clear, concise, and human-friendly responses for real estate queries

- **Reliable Translation Capabilities**
  - Enables accurate English → Spanish conversion without a separate translation model

- **Lower Latency vs Previous GPT-4 Models**
  - Important for multi-agent workflows where several calls may occur per request

GPT-4o allows the system to remain **intelligent without becoming brittle or slow**.

---

### 🏗️ Role of Azure AI Foundry

**Azure AI Foundry** acts as the **control plane** for the entire AI lifecycle in this project.

How it helps architecturally:

- **Model & Deployment Management**
  - Central place to manage chat models and embedding models
  - Clear mapping between deployments and application usage

- **Experimentation & Iteration**
  - Quickly test different models (e.g., embeddings vs chat)
  - Tune prompts and agent behavior without redeploying infrastructure

- **Consistency Across Environments**
  - Same deployment can be reused across dev, test, and prod
  - Reduces configuration drift

- **Observability**
  - Easier troubleshooting of latency, token usage, and failures

Azure AI Foundry ensures the AI layer is **operable, observable, and governable**, which is essential for real-world systems.

---

## 📁 Project 1: Web Scraping & Data Extraction

### Purpose
Reads a list of property URLs from a text file, loads each page using **Puppeteer Sharp**, and extracts structured property information.

### Input
- A text file containing real estate URLs, for example:
  
  ```
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443379952
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443112468
  https://www.realestate.com.au/property-studio-nsw-north+sydney-443208816
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-430326854
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443248604
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-442978384
  https://www.realestate.com.au/property-house-nsw-north+sydney-443369504
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443366516
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-437609036
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443365848
  https://www.realestate.com.au/property-unit-nsw-north+sydney-407471285
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443359504
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443354020
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-440119476
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443350708
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-423821986
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443241160
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443337596
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-442963792
  https://www.realestate.com.au/property-unit-nsw-north+sydney-441349032
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443326372
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-443073648
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-435190895
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-442944772
  https://www.realestate.com.au/property-apartment-nsw-north+sydney-442944324
  ```


### Extracted Data Model
```csharp
public class PropertyDetail
{
    public string Title { get; set; }
    public string Rooms { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }
    public List<string> NearbySchools { get; set; }
    public string AgentName { get; set; }
    public string Address { get; set; }
}
```

### Output
- A single **JSON file** containing a collection of `PropertyDetail` objects.
- This JSON file acts as the **source of truth** for downstream AI processing.

```
[
  {
    "Title": "703/9 William Street, North Sydney, NSW 2060",
    "Rooms": "Apartment  with 2 bedrooms  1 bathroom",
    "Status": "Available 25 Feb 2026",
    "Description": "Fully Furnished | Well-separated Bedroom | Timber Floor | Almost New Kitchen | Close to Everywhere\n703/9 WILLIAM STREET, NORTH SYDNEY\n\nEnjoy an exceptional Lower North Shore lifestyle in this bright, north-facing two-bedroom apartment, superbly positioned within the tightly held Bentleigh Apartments complex. Designed for comfort and privacy, the apartment features well-separated bedrooms, making it ideal for avoiding noise disturbance and perfect for professionals, couples, or students. Just moments from North Sydney CBD, the train station, Greenwood Plaza, caf\u00E9s, restaurants, and everyday amenities, this residence delivers outstanding convenience and comfort.\n\nApartment Features:\n\nModern kitchen with quality European appliances\n\nGenerous, sun-filled living and dining area\n\nTwo well-separated bedrooms, enhancing privacy and minimizing noise disturbance\n\nBuilt-in wardrobes in both bedrooms\n\nTwo balconies providing excellent outdoor space and abundant natural light\n\nSeparate air conditioning system for enhanced climate control and year-round comfort\n\nBuilding Amenities:\n\nSwimming pool\n\nFully equipped gymnasium\n\nBBQ and entertaining areas\n\nSecure, well-maintained complex\n\nPerfect for both relaxed living and entertaining, this apartment offers a seamless blend of comfort, style, privacy, and lifestyle appeal.\n\nLocated at 9 William Street, North Sydney, the property is set within a vibrant and well-connected precinct, offering easy access to North Sydney CBD, public transport, Greenwood Plaza, and a wide variety of caf\u00E9s and dining options, all within walking distance. Just a 2-minute walk to Victoria Metro Station, a 5-minute walk to North Sydney Train Station, 15 minutes to Chatswood, and only 10 minutes to the CBD.\n\nThis is a rare opportunity to secure a quality residence in one of North Sydney\u2019s most desirable locations. Contact us today to arrange an inspection.\n\nDisclaimer: All information has been obtained from sources deemed reliable; however, accuracy is not guaranteed. Interested parties are advised to conduct their own independent inquiries.\n\nRead more",
    "NearbySchools": [],
    "AgentName": "Lance (Lanze) XUE",
    "Address": "Horizon Point Realty21 16-20 Henley Road, HOMEBUSH WEST, NSW 2140"
  },
  {
    "Title": "7/199 Walker Street, North Sydney, NSW 2060",
    "Rooms": "Apartment  with   1 bathroom 1 car space",
    "Status": "Available now",
    "Description": "HOLDING DEPOSIT RECEIVED - Viewings cancelled\n7/199 WALKER STREET, NORTH SYDNEY\n\nBright Harbour-View Studio Perfect for the Busy Urban Lifestyle\n\nDesigned for those who want convenience, style, and easy access to everything North Sydney has to offer, this light-filled studio ticks all the boxes for a seamless modern lifestyle.\n\nJust a short walk to Victoria Cross Metro Station, this home gives you incredibly fast access to the CBD, Barangaroo, Chatswood, and the entire metro network - perfect for a quick commute or spontaneous nights out.\n\n- Sun-filled open layout with flexible living and bedroom zones\n- Private balcony with harbour \u0026 district views - your own quiet escape above the city\n- Well-designed kitchen with great storage and a harbour outlook from the window\n- Secure parking - a huge bonus in North Sydney\n- Quiet, secure building ideal for focused work-from-home days\n- Moments to Victoria Cross Metro, rail and buses for unbeatable connectivity\n- Walk to caf\u00E9s, gyms, wine bars, supermarkets \u0026 Greenwood Plaza\n- Low-maintenance lifestyle with everything you need right at your door\n\nThis studio offers the perfect balance of comfort, convenience and city energy - ideal for young professionals looking to upgrade their lifestyle in one of Sydney\u0027s best-connected locations.\n\nTo submit your application please visit snug.com/apply/holmesstclair\n\nPlease register for inspection updates as changes or cancellations may occur to inspection times.\n\nDisclaimer: All information contained herein is gathered from sources we believe reliable. We have no reason to doubt its accuracy, however, we cannot guarantee it. All interested parties should make \u0026 rely upon their own inquiries.\n\nRead more",
    "NearbySchools": [ "Wenona School Ltd", "St Mary\u0027s Catholic Primary School", "Cameragal Montessori School", "North Sydney Public School", "Sydney Church of England Grammar School" ],
    "AgentName": "George Marinho",
    "Address": "Holmes St. Clair - Crows Nest38 Willoughby Road, CROWS NEST, NSW 2065"
  },
  {
    "Title": "1109/79-81 Berry St, North Sydney, NSW 2060",
    "Rooms": "Studio  with   1 bathroom",
    "Status": "Available now",
    "Description": "Fully Furnished Apartment in Alexander Building - \u0027Deposit Taken\u0022\n1109/79-81 BERRY ST, NORTH SYDNEY\n\n* Deposit Taken*\nMove straight in and enjoy effortless city living in this well presented apartment, perfectly positioned in the highly popular Alexander Building.\n\nFreshly painted and filled with natural light, this sunny property is thoughtfully designed for comfort and convenience. The modern kitchen features gas cooking, a dishwasher and plenty storage, while the internal laundry includes both a washing machine and dryer. The security apartment comes fully furnished, complete with a brand new TV, air con, making it ideal for anyone wanting to move in with just a suitcase.\nStep out onto the cute east-facing balcony, the perfect spot to unwind after a long day.\n\nSituated right next to Metro Station (Victoria Cross), with countless caf\u00E9s and fine restaurants literally downstairs, everything you need is at your doorstep.\n\nAlexander Building is also equipped with swimming pool, gym, spa and 24-hour Concierge.\nAvailable now\n\nRead more",
    "NearbySchools": [ "Wenona School Ltd", "Sydney Church of England Grammar School", "North Sydney Public School", "St Mary\u0027s Catholic Primary School", "Cameragal Montessori School" ],
    "AgentName": "Melinda Wong",
    "Address": "Amazing Realty904/121 Walker Street, NORTH SYDNEY, NSW 2060"
  },
  .................
  ................
```
---

## 📁 Project 2: AI Agents, Embeddings & RAG

### Purpose
Implements a **multi-agent AI system** using **Microsoft Agent Framework** to answer user questions based on scraped real estate data.

---

## 🤖 AI Agents Overview

### 1️⃣ Orchestrator Agent
- Acts as the **central decision-maker**.
- Analyzes user intent and routes the request to the appropriate agent.
- Uses **CreateHandoffBuilderWithStrategy** for inter-agent communication.

### 2️⃣ RAG (Retrieval-Augmented Generation) Agent
- Queries the **in-memory vector store**.
- Retrieves the most relevant property data based on semantic similarity.
- Returns grounded answers strictly based on stored property information.

### 3️⃣ Translation Agent
- Triggered only when the orchestrator detects a translation request.
- Converts the **final RAG response** from **English to Spanish**.

---

## 🧠 Embedding & Vector Storage

### Embedding Utility
- Reads the JSON output from Project 1.
- Converts each property record into embeddings using:
  - **`text-embedding-small`** model
- Stores embeddings in an **in-memory vector database**.

### Why In-Memory Vector Store?
- Fast prototyping
- Low-latency semantic search
- Ideal for demos and local experimentation

---

## 🔄 Query Flow Example

1. User asks:
   > "Show me apartments near good schools in North Sydney"

2. Orchestrator Agent:
   - Detects informational query
   - Routes to RAG Agent

3. RAG Agent:
   - Performs vector similarity search
   - Retrieves relevant property records

4. (Optional) User asks:
   > "Translate this to Spanish"

5. Orchestrator:
   - Routes output to Translation Agent

6. Translation Agent:
   - Converts English response → Spanish

---

## 🎥 Demo
<video src="https://github.com/2010x25/real-estate-agents/blob/main/demo.mp4" width="600" controls></video>


## 🛠️ Tech Stack

- **Language**: C# (.NET)
- **Web Scraping**: Puppeteer Sharp
- **AI Framework**: Microsoft Agent Framework
- **Embeddings**: text-embedding-small
- **Vector Store**: In-Memory
- **Data Format**: JSON

---

## 🚀 Getting Started

### Prerequisites
- .NET SDK
- Chrome / Chromium (for Puppeteer Sharp)
- Azure OpenAI or OpenAI-compatible endpoint

### Steps
1. Run **Project 1** to scrape property data and generate the JSON file.
2. Configure embedding and AI credentials.
3. Run **Project 2** to:
   - Load embeddings into vector storage
   - Start the AI agent orchestration
4. Send queries to the orchestrator agent.

---

## 📌 Key Highlights

- Clean separation of **data ingestion** and **AI reasoning**
- Multi-agent orchestration with clear responsibilities
- RAG-based answers grounded in real data
- Extensible design (easy to add more agents)

---

## 📄 License

This project is for educational and experimental purposes. Please review the terms of service of the data sources and AI models used.

---

## 🙌 Contributions

Contributions, improvements, and suggestions are welcome. Feel free to open issues or pull requests.

---

**Happy Coding & Exploring AI Agents! 🚀**

