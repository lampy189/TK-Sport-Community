"""Collect actual test runner outputs; usage: python3 .../collect-results.py /path/to/trx."""
import html
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

repo = Path(__file__).resolve().parents[3]
destination = repo / "docs/evidence/duc-nov-07-10"
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
runs = []
for name in ["duc-api", "duc-payment"]:
    tree = ET.parse(Path(sys.argv[1]) / (name + ".trx"))
    counters = tree.find(".//t:Counters", ns).attrib
    cases = [dict(name=e.attrib["testName"], outcome=e.attrib["outcome"])
             for e in tree.findall(".//t:UnitTestResult", ns)]
    counters["skipped"] = str(sum(c["outcome"] == "NotExecuted" for c in cases))
    runs.append(dict(name=name, counters=counters, cases=cases))
ui = json.loads((destination / "ui-results.json").read_text())
output = dict(backend=runs, ui=ui["stats"])
(destination / "results.json").write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n")
rows = []
for run in runs:
    c = run["counters"]
    rows.append((run["name"], c["passed"], c["failed"], c["skipped"]))
rows.append(("Chrome component tests (API fixture)", ui["stats"]["expected"], ui["stats"]["unexpected"], ui["stats"]["skipped"]))
body = "".join("<tr>" + "".join("<td>" + html.escape(str(cell)) + "</td>" for cell in row) + "</tr>" for row in rows)
document = """<!doctype html><html lang="vi"><meta charset="utf-8"><title>Đức — kết quả kiểm thử</title>
<style>body{font:18px system-ui;background:#f3f7f3;color:#163d30;padding:40px;max-width:1100px;margin:auto}h1{font-size:30px}table{border-collapse:collapse;width:100%;background:white}td,th{padding:16px;border:1px solid #bcd0c3;text-align:left}th{background:#d9ecdf}p{line-height:1.6}.note{padding:18px;background:#fff4d7;border-radius:10px}</style>
<h1>ĐỨC — Kiểm thử Sự cố, Đổi/Hoàn vé, Ngoại lệ & Khiếu nại</h1>
<p>Nhánh DucTM · Jira TSC-437–TSC-441 · 09/11/2026 (Asia/Ho_Chi_Minh)</p>
<table><thead><tr><th>Bộ kiểm thử</th><th>Đạt</th><th>Lỗi</th><th>Bỏ qua</th></tr></thead><tbody>""" + body + """</tbody></table>
<p>Node unit tests: buildIssues — 4/4 đạt. Backend build: 0 cảnh báo, 0 lỗi. Frontend build: đạt.</p>
<p class="note">3 test SQL Server bị bỏ qua vì chưa cấu hình TKSC_TEST_SQL_CONNECTION.
Các test báo cáo dùng EF InMemory; test giao diện dùng API fixture và component thật.
Kết quả này chưa xác minh giao dịch ngân hàng, Google OAuth hoặc database SQL Server thật.</p>
<p>Dữ liệu số ca lấy từ TRX và JSON do test runner tạo; xem results.json và ui-results.json để đối chiếu.</p></html>"""
(destination / "results.html").write_text(document)
print(json.dumps(dict(runs=rows), ensure_ascii=False))
