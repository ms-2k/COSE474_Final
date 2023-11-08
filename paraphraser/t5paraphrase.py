import torch
from transformers import T5Tokenizer, T5ForConditionalGeneration

#acquire model and tokenizer
tokenizer = T5Tokenizer.from_pretrained("google/flan-t5-large")
model = T5ForConditionalGeneration.from_pretrained(
    "google/flan-t5-large",
    device_map="auto",
    load_in_8bit=True
)

#check if cuda is available and set device to use
if torch.cuda.is_available():
    device = 'cuda'
else:
    print('CUDA unavailable, using CPU')
    device = 'cpu'

#acquire text to paraphrase
original = input()
print()

#do until exit command given
while (original != 'exit'):
    
    #acquire word count
    word_count = original.count(' ')
    
    #tokenize and send to device
    token = tokenizer(
        'Paraphrase the following:\n' + original,
        return_tensors='pt'
    ).input_ids.to(device)
    
    #generate output, with similar length as original
    outputs = model.generate(
        token,
        min_length=int(word_count * 0.9),
        max_length=int(word_count * 1.5)
    )
    
    #output generated text
    print(tokenizer.decode(outputs[0]))
    
    #acquire new text to paraphrase
    original = input()
    print()