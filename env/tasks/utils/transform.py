import yaml
from openpyxl import load_workbook, Workbook


def read_yaml_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        data = yaml.safe_load(file)
    return data


def write_to_excel(data, excel_file_path, sheet_name):
    try:
   
        workbook = load_workbook(excel_file_path)
    except FileNotFoundError:
       
        workbook = Workbook()
     
        if 'Sheet' in workbook.sheetnames:
            del workbook['Sheet']

    if sheet_name in workbook.sheetnames:
        del workbook[sheet_name]

    worksheet = workbook.create_sheet(title=sheet_name)

    headers = ["Name", "ID", "Object", "Quantity", "Tool", "Save", "Init Commands", "Evaluator", "Difficulty"]
    for col, header in enumerate(headers, start=1):
        worksheet.cell(row=1, column=col, value=header)

    for row_num, (task_name, task_data) in enumerate(data.items(), start=2):
        worksheet.cell(row=row_num, column=1, value=task_name)
        worksheet.cell(row=row_num, column=2, value=task_data.get('id', ''))
        worksheet.cell(row=row_num, column=3, value=task_data.get('object', ''))
        worksheet.cell(row=row_num, column=4, value=task_data.get('quantity', ''))
        worksheet.cell(row=row_num, column=5, value=task_data.get('tool', ''))
        worksheet.cell(row=row_num, column=6, value=task_data.get('save', ''))

   
        init_commands = task_data.get('init_commands', [])
        init_commands_str = ", ".join(map(str, init_commands)) if isinstance(init_commands, list) else init_commands
        worksheet.cell(row=row_num, column=7, value=init_commands_str)

        worksheet.cell(row=row_num, column=8, value=task_data.get('evaluator', ''))
        worksheet.cell(row=row_num, column=9, value=task_data.get('difficulty', ''))

    workbook.save(excel_file_path)


if __name__ == "__main__":
    yaml_file_path = "../task_suite/combat_lite.yaml"
    excel_file_path = "../task_suite/StarDojo_Lite.xlsx"

    data = read_yaml_file(yaml_file_path)
    write_to_excel(data, excel_file_path, "Combat")

    print(f"data write: {excel_file_path}")
